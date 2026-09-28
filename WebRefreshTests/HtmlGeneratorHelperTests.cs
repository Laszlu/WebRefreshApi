using Shouldly;
using WebRefreshSiteBuilderAPI.Helpers;

namespace WebRefreshTests;

public class HtmlGeneratorHelperTests
{
    [Fact]
    public void ParseFiles_JsonEscapedCodeBlock_DecodesHtml()
    {
        const string response = "```html:index.html\n<!doctype html>\\n<html lang=\\\"de\\\">\\n</html>\n```";

        var file = HtmlGeneratorHelper.ParseFiles(response).ShouldHaveSingleItem();

        file.Content.ShouldBe("<!doctype html>\n<html lang=\"de\">\n</html>\n");
    }
}
