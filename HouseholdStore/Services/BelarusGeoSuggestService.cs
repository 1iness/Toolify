using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace HouseholdStore.Services;

public sealed class BelarusGeoSuggestService
{
    private static readonly string[] FallbackCities =
    [
        "Минск", "Гомель", "Могилёв", "Витебск", "Гродно", "Брест",
        "Барановичи", "Бобруйск", "Борисов", "Пинск", "Орша", "Мозырь",
        "Солигорск", "Новополоцк", "Лида", "Молодечно", "Полоцк",
        "Жлобин", "Светлогорск", "Речица", "Жодино", "Слуцк", "Кобрин",
        "Слоним", "Волковыск", "Калинковичи", "Сморгонь", "Рогачёв"
    ];

    private static readonly Dictionary<string, string[]> FallbackStreetsByCity = new(StringComparer.CurrentCultureIgnoreCase)
    {
        ["Минск"] =
        [
            "улица Семашко",
            "улица Колесникова",
            "1-й переулок Колесникова",
            "2-й переулок Колесникова",
            "3-й переулок Колесникова",
            "улица Немига",
            "проспект Независимости",
            "проспект Победителей",
            "улица Притыцкого",
            "улица Сурганова",
            "улица Якуба Коласа",
            "улица Максима Богдановича",
            "улица Кальварийская",
            "улица Орловская",
            "улица Маяковского"
        ]
    };

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BelarusGeoSuggestService> _logger;
    private readonly string _cacheDir;

    public BelarusGeoSuggestService(
        HttpClient http,
        IMemoryCache cache,
        ILogger<BelarusGeoSuggestService> logger,
        IWebHostEnvironment env)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
        _cacheDir = Path.Combine(env.ContentRootPath, "App_Data", "geo-cache");
        Directory.CreateDirectory(_cacheDir);

        _http.BaseAddress ??= new Uri("https://nominatim.openstreetmap.org/");
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
        {
            _http.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("ToolifyHouseholdStore", "1.0"));
        }
    }

    public async Task<IReadOnlyList<string>> SuggestCitiesAsync(string term, int limit = 10)
    {
        term = NormalizeTerm(term);
        limit = Math.Clamp(limit, 1, 50);
        if (term.Length == 0) return [];

        var cacheKey = $"by:cities:{term}:{limit}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);

            var localPlaces = await GetBelarusPlacesAsync();
            var local = RankMatches(localPlaces, term, limit).ToList();
            if (local.Count > 0) return local;

            var fallback = FallbackCities
                .Where(c => c.StartsWith(term, StringComparison.CurrentCultureIgnoreCase)
                    || c.Contains(term, StringComparison.CurrentCultureIgnoreCase))
                .Take(limit)
                .ToList();

            var photon = await TryPhotonAsync(
                "https://photon.komoot.io/api/"
                + $"?q={Uri.EscapeDataString(term)}&lang=ru&limit={Math.Min(limit * 4, 100)}"
                + "&osm_tag=place:city&osm_tag=place:town&osm_tag=place:village&osm_tag=place:hamlet",
                props => ReadPropertyValue(props, "name"),
                props => IsBelarusPhotonResult(props));

            var remote = await TryNominatimAsync(
                $"search?format=jsonv2&addressdetails=1&countrycodes=by&accept-language=ru&limit={Math.Min(limit * 3, 50)}&q={Uri.EscapeDataString(term + ", Беларусь")}",
                item => ReadAddressValue(item, "city", "town", "village", "hamlet", "municipality", "locality")
                    ?? FirstDisplayNamePart(item));

            return photon
                .Concat(remote)
                .Concat(fallback)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .Take(limit)
                .ToList();
        }) ?? [];
    }

    public async Task<IReadOnlyList<string>> SuggestStreetsAsync(string city, string term, int limit = 10)
    {
        city = NormalizeTerm(city);
        term = NormalizeTerm(term);
        limit = Math.Clamp(limit, 1, 50);
        if (city.Length < 2 || term.Length == 0) return [];

        var cacheKey = $"by:streets:{city}:{term}:{limit}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);

            var localStreets = await GetCityStreetsAsync(city);
            var fallbackStreets = GetFallbackStreets(city);
            var local = RankMatches(localStreets.Concat(fallbackStreets), term, limit, removeStreetPrefix: true).ToList();
            if (local.Count > 0) return local;

            var directOverpass = await TryLoadCityStreetMatchesFromOverpassAsync(city, term);
            var direct = RankMatches(directOverpass.Concat(fallbackStreets), term, limit, removeStreetPrefix: true).ToList();
            if (direct.Count > 0) return direct;

            var photon = await TryPhotonAsync(
                "https://photon.komoot.io/api/"
                + $"?q={Uri.EscapeDataString(term + " " + city)}&lang=ru&limit={Math.Min(limit * 6, 100)}&osm_tag=highway",
                props => ReadPropertyValue(props, "name"),
                props => IsBelarusPhotonResult(props));

            var photonPrefixed = await TryPhotonAsync(
                "https://photon.komoot.io/api/"
                + $"?q={Uri.EscapeDataString("улица " + term + " " + city)}&lang=ru&limit={Math.Min(limit * 6, 100)}&osm_tag=highway",
                props => ReadPropertyValue(props, "name"),
                props => IsBelarusPhotonResult(props));

            var structured = await TryNominatimAsync(
                "search?format=jsonv2&addressdetails=1&countrycodes=by&accept-language=ru"
                + $"&limit={Math.Min(limit * 4, 50)}&street={Uri.EscapeDataString(term)}&city={Uri.EscapeDataString(city)}&country={Uri.EscapeDataString("Беларусь")}",
                item => ReadAddressValue(item, "road", "pedestrian", "footway", "residential", "path")
                    ?? FirstDisplayNamePart(item));

            var query = $"{term}, {city}, Беларусь";
            var freeText = await TryNominatimAsync(
                $"search?format=jsonv2&addressdetails=1&countrycodes=by&accept-language=ru&limit={Math.Min(limit * 4, 50)}&q={Uri.EscapeDataString(query)}",
                item => ReadAddressValue(item, "road", "pedestrian", "footway", "residential", "path")
                    ?? FirstDisplayNamePart(item));

            var prefixed = await TryNominatimAsync(
                $"search?format=jsonv2&addressdetails=1&countrycodes=by&accept-language=ru&limit={Math.Min(limit * 4, 50)}&q={Uri.EscapeDataString("улица " + term + ", " + city + ", Беларусь")}",
                item => ReadAddressValue(item, "road", "pedestrian", "footway", "residential", "path")
                    ?? FirstDisplayNamePart(item));

            return photon
                .Concat(photonPrefixed)
                .Concat(structured)
                .Concat(freeText)
                .Concat(prefixed)
                .Where(x => LooksLikeStreet(x))
                .Select(NormalizeStreetName)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderByDescending(x => StartsWithStreetTerm(x, term))
                .ThenBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                .Take(limit)
                .ToList();
        }) ?? [];
    }

    private async Task<IReadOnlyList<string>> GetBelarusPlacesAsync()
    {
        const string memoryKey = "by:places:all:v1";
        if (_cache.TryGetValue(memoryKey, out IReadOnlyList<string>? cached) && cached != null)
            return cached;

        var file = Path.Combine(_cacheDir, "belarus-places.json");
        var fromFile = await TryReadCachedListAsync(file, TimeSpan.FromDays(30));
        if (fromFile.Count > 0)
        {
            _cache.Set(memoryKey, fromFile, TimeSpan.FromHours(12));
            return fromFile;
        }

        var fromOverpass = await TryLoadBelarusPlacesFromOverpassAsync();
        var result = fromOverpass.Count > 0
            ? fromOverpass
            : FallbackCities.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();

        await TryWriteCachedListAsync(file, result);
        _cache.Set(memoryKey, result, TimeSpan.FromHours(12));
        return result;
    }

    private async Task<IReadOnlyList<string>> GetCityStreetsAsync(string city)
    {
        var normalizedCity = NormalizeForCompare(city);
        if (normalizedCity.Length < 2) return [];

        var memoryKey = $"by:streets:all:{normalizedCity}";
        if (_cache.TryGetValue(memoryKey, out IReadOnlyList<string>? cached) && cached != null)
            return cached;

        var file = Path.Combine(_cacheDir, "streets-" + ToSafeFileKey(normalizedCity) + ".json");
        var fromFile = await TryReadCachedListAsync(file, TimeSpan.FromDays(30));
        if (fromFile.Count > 0)
        {
            _cache.Set(memoryKey, fromFile, TimeSpan.FromHours(12));
            return fromFile;
        }

        var fromOverpass = await TryLoadCityStreetsFromOverpassAsync(city);
        if (fromOverpass.Count > 0)
        {
            await TryWriteCachedListAsync(file, fromOverpass);
            _cache.Set(memoryKey, fromOverpass, TimeSpan.FromHours(12));
        }

        return fromOverpass;
    }

    private async Task<List<string>> TryLoadBelarusPlacesFromOverpassAsync()
    {
        const string query = """
            [out:json][timeout:80];
            area["ISO3166-1"="BY"][admin_level=2]->.by;
            (
              node["place"~"^(city|town|village|hamlet)$"](area.by);
              way["place"~"^(city|town|village|hamlet)$"](area.by);
              relation["place"~"^(city|town|village|hamlet)$"](area.by);
            );
            out tags;
            """;

        return await TryOverpassNamesAsync(query);
    }

    private async Task<List<string>> TryLoadCityStreetsFromOverpassAsync(string city)
    {
        var escapedCity = EscapeOverpassString(city);
        var query = $$"""
            [out:json][timeout:80];
            area["ISO3166-1"="BY"][admin_level=2]->.by;
            (
              area["name"="{{escapedCity}}"]["boundary"="administrative"](area.by);
              area["name:ru"="{{escapedCity}}"]["boundary"="administrative"](area.by);
              area["name"="{{escapedCity}}"]["place"~"^(city|town|village|hamlet)$"](area.by);
              area["name:ru"="{{escapedCity}}"]["place"~"^(city|town|village|hamlet)$"](area.by);
            )->.cityArea;
            way(area.cityArea)["highway"]["name"];
            out tags;
            """;

        return (await TryOverpassNamesAsync(query))
            .Where(LooksLikeStreet)
            .Select(NormalizeStreetName)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(x => RemoveStreetPrefix(x), StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private async Task<List<string>> TryLoadCityStreetMatchesFromOverpassAsync(string city, string term)
    {
        var escapedCity = EscapeOverpassString(city);
        var escapedTerm = EscapeOverpassRegex(term);
        if (escapedTerm.Length == 0) return [];

        var query = $$"""
            [out:json][timeout:45];
            area["ISO3166-1"="BY"][admin_level=2]->.by;
            (
              area["name"="{{escapedCity}}"]["boundary"="administrative"](area.by);
              area["name:ru"="{{escapedCity}}"]["boundary"="administrative"](area.by);
              area["name"="{{escapedCity}}"]["place"~"^(city|town|village|hamlet)$"](area.by);
              area["name:ru"="{{escapedCity}}"]["place"~"^(city|town|village|hamlet)$"](area.by);
            )->.cityArea;
            (
              way(area.cityArea)["highway"]["name"~"{{escapedTerm}}",i];
              way(area.cityArea)["highway"]["name:ru"~"{{escapedTerm}}",i];
            );
            out tags;
            """;

        return (await TryOverpassNamesAsync(query))
            .Where(LooksLikeStreet)
            .Select(NormalizeStreetName)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(x => RemoveStreetPrefix(x), StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private async Task<List<string>> TryOverpassNamesAsync(string query)
    {
        try
        {
            using var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", query)
            });
            using var response = await _http.PostAsync("https://overpass-api.de/api/interpreter", content);
            if (!response.IsSuccessStatusCode) return [];

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            if (!doc.RootElement.TryGetProperty("elements", out var elements)
                || elements.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<string>();
            foreach (var element in elements.EnumerateArray())
            {
                if (!element.TryGetProperty("tags", out var tags)
                    || tags.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = ReadPropertyValue(tags, "name:ru", "name");
                if (!string.IsNullOrWhiteSpace(name))
                    result.Add(name.Trim());
            }

            return result
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Overpass geo directory load failed.");
            return [];
        }
    }

    private async Task<List<string>> TryReadCachedListAsync(string path, TimeSpan maxAge)
    {
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists || DateTime.UtcNow - file.LastWriteTimeUtc > maxAge) return [];

            await using var stream = file.OpenRead();
            return await JsonSerializer.DeserializeAsync<List<string>>(stream) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Geo cache read failed: {Path}", path);
            return [];
        }
    }

    private async Task TryWriteCachedListAsync(string path, IReadOnlyList<string> values)
    {
        try
        {
            var tmp = path + ".tmp";
            await using (var stream = File.Create(tmp))
            {
                await JsonSerializer.SerializeAsync(stream, values);
            }

            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Geo cache write failed: {Path}", path);
        }
    }

    private static IEnumerable<string> RankMatches(
        IEnumerable<string> values,
        string term,
        int limit,
        bool removeStreetPrefix = false)
    {
        var normalizedTerm = NormalizeForCompare(removeStreetPrefix ? RemoveStreetPrefix(term) : term);
        if (normalizedTerm.Length == 0) return [];

        return values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .Select(v => new
            {
                Value = v,
                Comparable = NormalizeForCompare(removeStreetPrefix ? RemoveStreetPrefix(v) : v)
            })
            .Where(x => x.Comparable.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Comparable.StartsWith(normalizedTerm, StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.Comparable.IndexOf(normalizedTerm, StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.Value, StringComparer.CurrentCultureIgnoreCase)
            .Take(limit)
            .Select(x => x.Value);
    }

    private async Task<List<string>> TryNominatimAsync(string relativeUrl, Func<JsonElement, string?> selector)
    {
        try
        {
            using var response = await _http.GetAsync(relativeUrl);
            if (!response.IsSuccessStatusCode) return [];

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];

            var result = new List<string>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var value = selector(item);
                if (!string.IsNullOrWhiteSpace(value))
                    result.Add(value);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Belarus geo suggestions failed.");
            return [];
        }
    }

    private async Task<List<string>> TryPhotonAsync(
        string url,
        Func<JsonElement, string?> selector,
        Func<JsonElement, bool>? predicate = null)
    {
        try
        {
            using var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return [];

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            if (!doc.RootElement.TryGetProperty("features", out var features)
                || features.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<string>();
            foreach (var feature in features.EnumerateArray())
            {
                if (!feature.TryGetProperty("properties", out var props)
                    || props.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (predicate != null && !predicate(props)) continue;

                var value = selector(props);
                if (!string.IsNullOrWhiteSpace(value))
                    result.Add(value);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Photon geo suggestions failed.");
            return [];
        }
    }

    private static string NormalizeTerm(string? value) =>
        (value ?? string.Empty).Trim();

    private static string? ReadAddressValue(JsonElement item, params string[] keys)
    {
        if (!item.TryGetProperty("address", out var address)
            || address.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var key in keys)
        {
            if (address.TryGetProperty(key, out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static string? FirstDisplayNamePart(JsonElement item)
    {
        if (!item.TryGetProperty("display_name", out var display)
            || display.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return display.GetString()?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.Trim();
    }

    private static string? ReadPropertyValue(JsonElement properties, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (properties.TryGetProperty(key, out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static bool IsBelarusPhotonResult(JsonElement properties)
    {
        var code = ReadPropertyValue(properties, "countrycode");
        return string.Equals(code, "BY", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeSameCity(JsonElement properties, string city)
    {
        var normalizedCity = NormalizeForCompare(city);
        if (normalizedCity.Length == 0) return true;

        var places = new[]
        {
            ReadPropertyValue(properties, "city"),
            ReadPropertyValue(properties, "town"),
            ReadPropertyValue(properties, "village"),
            ReadPropertyValue(properties, "locality"),
            ReadPropertyValue(properties, "district"),
            ReadPropertyValue(properties, "county"),
            ReadPropertyValue(properties, "state")
        };

        return places.Any(p =>
            !string.IsNullOrWhiteSpace(p)
            && NormalizeForCompare(p).Contains(normalizedCity, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeForCompare(string? value) =>
        (value ?? string.Empty)
            .Replace("ё", "е", StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToLower(CultureInfo.GetCultureInfo("ru-RU"));

    private static string NormalizeStreetName(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? trimmed : trimmed;
    }

    private static bool StartsWithStreetTerm(string value, string term)
    {
        var street = NormalizeForCompare(RemoveStreetPrefix(value));
        var query = NormalizeForCompare(RemoveStreetPrefix(term));
        return query.Length > 0 && street.StartsWith(query, StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoveStreetPrefix(string value)
    {
        var result = (value ?? string.Empty).Trim();
        var prefixes = new[]
        {
            "улица ", "ул. ", "ул ", "проспект ", "пр-т ", "переулок ",
            "пер. ", "проезд ", "шоссе ", "тракт ", "бульвар ", "площадь "
        };

        foreach (var prefix in prefixes)
        {
            if (result.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
                return result[prefix.Length..].Trim();
        }

        return result;
    }

    private static string ToSafeFileKey(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string EscapeOverpassString(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string EscapeOverpassRegex(string value) =>
        Regex.Escape((value ?? string.Empty).Trim())
            .Replace("\"", "\\\"", StringComparison.Ordinal);

    private static IReadOnlyList<string> GetFallbackStreets(string city)
    {
        foreach (var pair in FallbackStreetsByCity)
        {
            if (NormalizeForCompare(pair.Key) == NormalizeForCompare(city))
                return pair.Value;
        }

        return [];
    }

    private static bool LooksLikeStreet(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var lower = value.ToLower(CultureInfo.GetCultureInfo("ru-RU"));
        return lower.Contains("улица")
            || lower.Contains("ул.")
            || lower.Contains("проспект")
            || lower.Contains("пр-т")
            || lower.Contains("переулок")
            || lower.Contains("проезд")
            || lower.Contains("шоссе")
            || lower.Contains("тракт")
            || lower.Contains("бульвар")
            || lower.Contains("площадь")
            || lower.Length > 2;
    }
}
