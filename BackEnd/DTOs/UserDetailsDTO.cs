namespace BackEnd.DTOs
{
    public class UserDetailsDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string Gender { get; set; } = string.Empty;

        public string DOB { get; set; } = string.Empty;


        public IFormFile? Image { get; set; } = null;
    }
}
