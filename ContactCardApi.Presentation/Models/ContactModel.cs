namespace ContactCardApi.Presentation.Models;

public class ContactModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Title { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string? LinkedIn { get; set; }
    public string? Twitter { get; set; }
    public string? GitHub { get; set; }
}
