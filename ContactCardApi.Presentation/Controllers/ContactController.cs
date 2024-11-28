using ContactCardApi.Presentation.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using ZXing;

namespace ContactCardApi.Presentation.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContactController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private static int _currentId = 1;
    private const int MaxCards = 10;

    public ContactController(IMemoryCache cache)
    {
        _cache = cache;
    }

    [HttpGet]
    public IActionResult GetAllContacts()
    {
        var contactos = _cache.Get<List<ContactModel>>("contactos");
        if (contactos == null || !contactos.Any())
        {
            return NotFound("No contacts found.");
        }
        return Ok(contactos);
    }

    [HttpGet("{id}")]
    public IActionResult GetContact(int id)
    {
        var contactos = _cache.Get<List<ContactModel>>("contactos");
        if (contactos == null || !contactos.Any(c => c.Id == id))
        {
            return NotFound("Contact not found.");
        }

        var contact = contactos.First(c => c.Id == id);
        return Ok(contact);
    }

    [HttpPost]
    public IActionResult CreateContact([FromBody] ContactModel contact)
    {
        var contactos = _cache.Get<List<ContactModel>>("contactos") ?? new List<ContactModel>();

        if (contactos.Count >= MaxCards)
        {
            return BadRequest($"Cannot store more than {MaxCards} contacts.");
        }

        contact.Id = _currentId++;
        contactos.Add(contact);

        _cache.Set("contactos", contactos, TimeSpan.FromHours(24));

        return CreatedAtAction(nameof(GetContact), new { id = contact.Id }, contact);
    }

    [HttpPut("{id}")]
    public IActionResult EditContact(int id, [FromBody] ContactModel updatedContact)
    {
        var contactos = _cache.Get<List<ContactModel>>("contactos");
        if (contactos == null || !contactos.Any(c => c.Id == id))
        {
            return NotFound("Contact not found.");
        }

        var contact = contactos.First(c => c.Id == id);
        contact.Name = updatedContact.Name;
        contact.Phone = updatedContact.Phone;
        contact.Email = updatedContact.Email;
        contact.LinkedIn = updatedContact.LinkedIn;
        contact.Twitter = updatedContact.Twitter;
        contact.GitHub = updatedContact.GitHub;

        _cache.Set("contactos", contactos, TimeSpan.FromHours(24));

        return Ok(contact);
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteContact(int id)
    {
        var contactos = _cache.Get<List<ContactModel>>("contactos");
        if (contactos == null || !contactos.Any(c => c.Id == id))
        {
            return NotFound("Contact not found.");
        }

        var contact = contactos.First(c => c.Id == id);
        contactos.Remove(contact);

        _cache.Set("contactos", contactos, TimeSpan.FromHours(24));

        return NoContent();
    }

    [HttpGet("{id}/vcard")]
    public IActionResult GetVCard(int id)
    {
        var contactos = _cache.Get<List<ContactModel>>("contactos");
        if (contactos == null || !contactos.Any(c => c.Id == id))
        {
            return NotFound("Contact not found.");
        }

        var contact = contactos.First(c => c.Id == id);
        var vCardBuilder = new StringBuilder();
        vCardBuilder.AppendLine("BEGIN:VCARD");
        vCardBuilder.AppendLine("VERSION:3.0");
        vCardBuilder.AppendLine($"FN:{contact.Name}");
        vCardBuilder.AppendLine($"TITLE:{contact.Title}");
        vCardBuilder.AppendLine($"TEL:{contact.Phone}");
        vCardBuilder.AppendLine($"EMAIL:{contact.Email}");
        vCardBuilder.AppendLine($"URL:{contact.LinkedIn}");
        vCardBuilder.AppendLine("END:VCARD");

        var vCardBytes = Encoding.UTF8.GetBytes(vCardBuilder.ToString());
        return File(vCardBytes, "text/vcard", "contact.vcf");
    }

    [HttpGet("generate")]
    public IActionResult GenerateQRCode([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return BadRequest("The URL parameter is required.");
        }

        try
        {
            var writer = new BarcodeWriterSvg
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = 300,
                    Width = 300,
                    Margin = 0
                }
            };

            var svgImage = writer.Write(url);

            var svgBytes = Encoding.UTF8.GetBytes(svgImage.Content);
            return File(svgBytes, "image/svg+xml");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }
}

