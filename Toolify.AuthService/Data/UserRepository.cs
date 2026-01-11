using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Toolify.AuthService.Models;

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

        var cmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Email=@email", con);
        cmd.Parameters.AddWithValue("@email", email);

        int count = (int)cmd.ExecuteScalar();
        return count > 0;
    }

    public void CreateUser(User user)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand(@"
            INSERT INTO Users (FirstName, LastName, Email, Phone, PasswordHash, Role, EmailConfirmed, EmailConfirmCode, EmailConfirmExpires)
            VALUES (@fn, @ln, @em, @ph, @pw, @role, @confirmed, @code, @expires)", con);

        cmd.Parameters.AddWithValue("@fn", user.FirstName);
        cmd.Parameters.AddWithValue("@ln", user.LastName);
        cmd.Parameters.AddWithValue("@em", user.Email);
        cmd.Parameters.AddWithValue("@ph", user.Phone);
        cmd.Parameters.AddWithValue("@pw", user.Password);
        cmd.Parameters.AddWithValue("@role", user.Role);
        cmd.Parameters.AddWithValue("@confirmed", user.EmailConfirmed);
        cmd.Parameters.AddWithValue("@code", user.EmailConfirmCode);
        cmd.Parameters.AddWithValue("@expires", user.EmailConfirmExpires);

        cmd.ExecuteNonQuery();
    }

    public User? GetUserByEmailAndPassword(string email, string password)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand(@"
        SELECT * FROM Users 
        WHERE Email=@em AND PasswordHash=@pw", con);

        cmd.Parameters.AddWithValue("@em", email);
        cmd.Parameters.AddWithValue("@pw", password);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return new User
        {
            Id = (int)reader["Id"],
            FirstName = reader["FirstName"].ToString()!,
            LastName = reader["LastName"].ToString()!,
            Email = reader["Email"].ToString()!,
            Phone = reader["Phone"].ToString()!,
            Password = reader["PasswordHash"].ToString()!,
            Role = reader["Role"].ToString()!
        };
    }
    public User? GetUserByEmail(string email)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand(@"
        SELECT * FROM Users 
        WHERE Email=@em", con);

        cmd.Parameters.AddWithValue("@em", email);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return new User
        {
            Id = (int)reader["Id"],
            FirstName = reader["FirstName"].ToString()!,
            LastName = reader["LastName"].ToString()!,
            Email = reader["Email"].ToString()!,
            Phone = reader["Phone"].ToString()!,
            Password = reader["PasswordHash"].ToString()!,
            Role = reader["Role"].ToString()!,
            EmailConfirmed = (bool)reader["EmailConfirmed"],
            EmailConfirmCode = reader["EmailConfirmCode"]?.ToString(),
            EmailConfirmExpires = reader["EmailConfirmExpires"] == DBNull.Value ? null : (DateTime)reader["EmailConfirmExpires"]
        };
    }
    public void ConfirmEmail(string email)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand(@"
        UPDATE Users
        SET EmailConfirmed = 1,
            EmailConfirmCode = NULL,
            EmailConfirmExpires = NULL
        WHERE Email = @em", con);

        cmd.Parameters.AddWithValue("@em", email);
        cmd.ExecuteNonQuery();
    }

    public void UpdateEmailConfirmCode(string email, string code, DateTime expires)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand(@"
        UPDATE Users
        SET EmailConfirmCode = @code,
            EmailConfirmExpires = @expires
        WHERE Email = @em", con);

        cmd.Parameters.AddWithValue("@em", email);
        cmd.Parameters.AddWithValue("@code", code);
        cmd.Parameters.AddWithValue("@expires", expires);

        cmd.ExecuteNonQuery();
    }

    public void SetPasswordResetCode(string email, string code, DateTime expires)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand(@"
        UPDATE Users
        SET PasswordResetCode=@code,
            PasswordResetExpires=@expires
        WHERE Email=@em", con);

        cmd.Parameters.AddWithValue("@em", email);
        cmd.Parameters.AddWithValue("@code", code);
        cmd.Parameters.AddWithValue("@expires", expires);

        cmd.ExecuteNonQuery();
    }

    public bool CheckResetCode(string email, string code)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand(@"
        SELECT COUNT(*) FROM Users
        WHERE Email=@em AND PasswordResetCode=@code AND PasswordResetExpires > GETUTCDATE()", con);

        cmd.Parameters.AddWithValue("@em", email);
        cmd.Parameters.AddWithValue("@code", code);

        return (int)cmd.ExecuteScalar() > 0;
    }

    public void UpdatePassword(string email, string newHash)
    {
        using var con = new SqlConnection(_connectionString);
        con.Open();

        var cmd = new SqlCommand(@"
        UPDATE Users
        SET PasswordHash=@pw,
            PasswordResetCode=NULL,
            PasswordResetExpires=NULL
        WHERE Email=@em", con);

        cmd.Parameters.AddWithValue("@em", email);
        cmd.Parameters.AddWithValue("@pw", newHash);

        cmd.ExecuteNonQuery();
    }

}
