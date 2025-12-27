using BackEnd.DTOs;
using BackEnd.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;


namespace BackEnd.Controller
{
    [ApiController]
    [Route("api/[controller]")]

    
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private static List<User> users = new();

        public UserController(IConfiguration config)
        {
            _config = config;
        }
        [HttpPost("register")]
        public IActionResult Register(RegisterDTO model)
        {
            if (users.Any(u => u.Email == model.Email))
                return BadRequest(new { message = "Email already registered" });

            var user = new User
            {
                Id = users.Count + 1,
                Name = model.Name,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
            };
            users.Add(user);

            return Ok(new { message = "Registration successful" });
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDTO model)
        {
            var user = users.SingleOrDefault(u => u.Email == model.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                return BadRequest(new { message = "Invalid Credentials" });
            var claims = new[]
            { new Claim(ClaimTypes.Email, user.Email)};

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                email = model.Email,
                name = user.Name
            });

        }
        
        [HttpPost("SaveUserDetails")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SaveUserDetails([FromForm] UserDetailsDTO model)
        {
           

            var filePath = Path.Combine("Data", "users.json");

            if (System.IO.File.Exists(filePath))
            {
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                users = JsonSerializer.Deserialize<List<User>>(json) ?? new();
            }
            var existingUser = users.SingleOrDefault(u => u.Email == model.Email);

            string imageFileName = existingUser?.ImageFileName ?? "";

            if (model.Image != null && model.Image.Length > 0)
            {

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                imageFileName = $"{Guid.NewGuid()}{Path.GetExtension(model.Image.FileName)}";
                var savePath = Path.Combine(uploadsFolder, imageFileName);
                using var stream = new FileStream(savePath, FileMode.Create);
                await model.Image.CopyToAsync(stream);

            }
            if (existingUser != null)
            {
               
                existingUser.Name = model.Name;
                existingUser.Gender = model.Gender;
                existingUser.DOB = model.DOB;
                existingUser.ImageFileName = imageFileName;
            }



            var updatedJson = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, updatedJson);

            return Ok(new
            {
                message = existingUser != null ? "User updated successfully" : "Registration successful",
                user = existingUser ?? users.Last()
            });
        }
    }
}
