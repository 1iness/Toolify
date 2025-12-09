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
            INSERT INTO Users (FirstName, LastName, Email, Phone, PasswordHash)
            VALUES (@fn, @ln, @em, @ph, @pw)", con);

        cmd.Parameters.AddWithValue("@fn", user.FirstName);
        cmd.Parameters.AddWithValue("@ln", user.LastName);
        cmd.Parameters.AddWithValue("@em", user.Email);
        cmd.Parameters.AddWithValue("@ph", user.Phone);
        cmd.Parameters.AddWithValue("@pw", user.Password);

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

}
