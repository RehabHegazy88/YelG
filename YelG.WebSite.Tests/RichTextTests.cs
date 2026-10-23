using YelG.WebSite.Rendering;

namespace YelG.WebSite.Tests
{
    public class RichTextTests
    {
        private static string Html(string text, RichTextOptions? options = null)
        {
            using var writer = new StringWriter();
            RichText.Render(text, options).WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
            return writer.ToString();
        }

        [Fact]
        public void Highlights_breaks_and_links_are_rendered_and_everything_else_is_encoded()
        {
            Assert.Equal("Nine services<br class=\"hide-sm\"> <em>one partner</em>",
                Html("Nine services\n*one partner*", new RichTextOptions { BreakClass = "hide-sm" }));
            Assert.Equal("Not found? <a href=\"#consult\">Send it</a>.", Html("Not found? [Send it](#consult)."));
            Assert.Equal("&lt;script&gt;alert(1)&lt;/script&gt; &amp; more", Html("<script>alert(1)</script> & more"));
        }

        [Fact]
        public void Unsafe_link_targets_stay_plain_text()
        {
            Assert.Equal("[x](javascript:alert(1))", Html("[x](javascript:alert(1))"));
            Assert.Equal("<a href=\"https://example.com\" target=\"_blank\" rel=\"noopener\">site</a>", Html("[site](https://example.com)"));
        }

        [Fact]
        public void An_escaped_asterisk_is_literal_and_can_be_coloured()
        {
            Assert.Equal("Fields marked * are required.", Html("Fields marked \\* are required."));
            Assert.Equal("Fields marked <span class=\"req\">*</span> are required.",
                Html("Fields marked \\* are required.", new RichTextOptions { MarkStar = true }));
            Assert.Equal("a * b", Html("a * b"));   // an unmatched asterisk is just a character
        }

        [Fact]
        public void Latin_runs_are_isolated_on_Arabic_pages_but_digits_are_not()
        {
            Assert.Equal("انطلقت <bdi class=\"lat\">YelG Advisory</bdi> عام 2017.",
                Html("انطلقت YelG Advisory عام 2017.", new RichTextOptions { IsolateLatin = true }));
            Assert.Equal("أنظمة <bdi class=\"lat\">ERP</bdi> و<bdi class=\"lat\">SaaS</bdi>",
                Html("أنظمة ERP وSaaS", new RichTextOptions { IsolateLatin = true }));
        }

        [Fact]
        public void Plain_text_drops_the_markup()
        {
            Assert.Equal("Send it now *", RichText.Plain("[Send it](#consult) *now* \\*"));
        }
    }
}
