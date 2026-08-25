using System.ComponentModel.DataAnnotations;

namespace SiteBuilderContracts.Requests;

public class HtmlAnalysisRequest
{
    [Required(ErrorMessage = "Source HTML is required")]
    public string Html { get; set; }
}