using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Toolify.AuthService.Models;
using System.Data;

namespace Toolify.AuthService.Data;

public class UserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }

    public bool UserExists(string email)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand("usp_UserExists", con)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("Email", email);
        int count = (int)cmd.ExecuteScalar();
        return count > 0;
    }

    public void CreateUser(User user)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand("usp_CreateUser", con)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@FirstName", user.FirstName);
        cmd.Parameters.AddWithValue("@LastName", user.LastName);
        cmd.Parameters.AddWithValue("@Email", user.Email);
        cmd.Parameters.AddWithValue("@Phone", user.Phone);
        cmd.Parameters.AddWithValue("@PasswordHash", user.Password);
        cmd.Parameters.AddWithValue("@Role", user.Role);
        cmd.Parameters.AddWithValue("@EmailConfirmed", user.EmailConfirmed);
        cmd.Parameters.AddWithValue("@EmailConfirmCode", (object?)user.EmailConfirmCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EmailConfirmExpires", (object?)user.EmailConfirmExpires ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public User? GetUserByEmailAndPassword(string email, string password)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand("usp_GetUserByEmailAndPassword", con)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@PasswordHash", password);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return MapUserFromReader(reader);
    }
    public User? GetUserByEmail(string email)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand("usp_GetUserByEmail", con)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Email", email);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return MapUserFromReader(reader);
    }
    public void ConfirmEmail(string email)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand("usp_ConfirmEmail", con)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Email", email);

        cmd.ExecuteNonQuery();
    }

    public void UpdateEmailConfirmCode(string email, string code, DateTime expires)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand("usp_UpdateEmailConfirmCode", con)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@Code", code);
        cmd.Parameters.AddWithValue("@Expires", expires);

        cmd.ExecuteNonQuery();
    }

    public void SetPasswordResetCode(string email, string code, DateTime expires)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand("usp_SetPasswordResetCode", con)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@Code", code);
        cmd.Parameters.AddWithValue("@Expires", expires);

        cmd.ExecuteNonQuery();
    }

    public bool CheckResetCode(string email, string code)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand("usp_CheckResetCode", con)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@Code", code);

        return (int)cmd.ExecuteScalar() > 0;
    }

    public void UpdatePassword(string email, string newHash)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand("usp_UpdatePassword", con)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@PasswordHash", newHash);

        cmd.ExecuteNonQuery();
    }
    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = new List<User>();

        using (var con = new SqlConnection(_connectionString))
        {
            using (var cmd = new SqlCommand("usp_GetAllUsers", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                await con.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(MapUserFromReader(reader));
                    }
                }
            }
        }

        return users;
    }

    public void UpdateUserProfile(string email, string firstName, string lastName, string phone)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand("usp_UpdateUserProfile", con)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@FirstName", firstName);
        cmd.Parameters.AddWithValue("@LastName", lastName);
        cmd.Parameters.AddWithValue("@Phone", phone);

        cmd.ExecuteNonQuery();
    }

    private User MapUserFromReader(SqlDataReader reader)
    {
        return new User
        {
            Id = (int)reader["Id"],
            FirstName = reader["FirstName"].ToString()!,
            LastName = reader["LastName"].ToString()!,
            Email = reader["Email"].ToString()!,
            Phone = reader["Phone"].ToString()!,
            Password = reader["PasswordHash"].ToString()!,
            Role = reader["Role"].ToString()!,
            EmailConfirmed = reader["EmailConfirmed"] != DBNull.Value && (bool)reader["EmailConfirmed"],
            EmailConfirmCode = reader["EmailConfirmCode"]?.ToString(),
            EmailConfirmExpires = reader["EmailConfirmExpires"] == DBNull.Value ? null : (DateTime)reader["EmailConfirmExpires"]
        };
    }
}
