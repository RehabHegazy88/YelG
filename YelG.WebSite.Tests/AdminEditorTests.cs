using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Areas.Admin;
using YelG.WebSite.Areas.Admin.Editing;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Enquiries;
using YelG.WebSite.Domain.Media;
using YelG.WebSite.Domain.Sections;
using YelG.WebSite.Infrastructure.Data;

namespace YelG.WebSite.Tests
{
    /// <summary>The admin screens' definitions and the form rules, without a database.</summary>
    public class AdminEditorTests
    {
        private static readonly EditorRegistry Registry = AdminScreens.Build();

        /// <summary>The model only (no connection is opened).</summary>
        private static Microsoft.EntityFrameworkCore.Metadata.IModel Model()
        {
            var options = new DbContextOptionsBuilder<WebSiteDbContext>().UseSqlServer("Server=none;Database=none").Options;
            using var db = new WebSiteDbContext(options);
            return db.Model;
        }

        [Fact]
        public void Every_field_of_every_screen_is_a_mapped_column()
        {
            var model = Model();
            foreach (var editor in Registry.Editors)
            {
                var entity = model.FindEntityType(editor.EntityType);
                Assert.True(entity is not null, $"{editor.EntityType.Name} is not mapped.");
                foreach (var field in editor.Fields)
                {
                    var mapped = field.IsLocalized
                        ? entity!.FindComplexProperty(field.Name) is not null
                        : entity!.FindProperty(field.Name) is not null;
                    Assert.True(mapped, $"{editor.EntityType.Name}.{field.Name} is not a mapped column.");
                }
            }
        }

        [Fact]
        public void Every_content_table_has_a_screen()
        {
            var notEdited = new[] { typeof(MediaAsset), typeof(Enquiry), typeof(EnquiryNote), typeof(Domain.Careers.JobApplication), typeof(Domain.Careers.JobApplicationNote) };   // their own screens
            var content = Model().GetEntityTypes().Select(t => t.ClrType)
                .Where(t => typeof(BaseEntity).IsAssignableFrom(t) && !notEdited.Contains(t));
            var edited = Registry.Editors.Select(e => e.EntityType).ToHashSet();
            Assert.All(content, t => Assert.True(edited.Contains(t), $"{t.Name} has no admin screen."));
        }

        [Fact]
        public void Every_text_and_image_of_a_section_is_editable()
        {
            // a LocalizedText or a media id that no screen shows would be content the admin cannot change
            foreach (var editor in Registry.Editors)
            {
                var editable = editor.Fields.Select(f => f.Name).ToHashSet();
                var expected = editor.EntityType.GetProperties()
                    .Where(p => p.CanWrite && (p.PropertyType == typeof(LocalizedText) || (p.Name.EndsWith("Id") && p.PropertyType == typeof(long?))))
                    .Select(p => p.Name);
                Assert.All(expected, name => Assert.True(editable.Contains(name), $"{editor.EntityType.Name}.{name} is not on its screen."));
            }
        }

        [Fact]
        public void Screens_and_lists_have_unique_addresses()
        {
            Assert.Equal(Registry.Screens.Count, Registry.Screens.Select(s => s.Slug).Distinct().Count());
            foreach (var screen in Registry.Screens)
            {
                var paths = screen.AllLists().Select(l => l.Path).ToList();
                Assert.Equal(paths.Count, paths.Distinct().Count());
                Assert.All(paths, p => Assert.Same(screen.AllLists().First(l => l.Path == p), screen.FindList(p)));
            }
            Assert.True(Registry.Find("settings")!.AdminOnly);
        }

        [Fact]
        public void Maximum_lengths_come_from_the_database_model()
        {
            var registry = AdminScreens.Build();
            registry.ApplyModel(Model());
            var credential = registry.Find("about")!.FindList("credentials")!.Item.Field(nameof(Credential.Code))!;
            Assert.Equal(20, credential.MaxLength);
            var stat = registry.Find("hero")!.FindList("stats")!.Item.Field(nameof(HeroStat.Value))!;
            Assert.Equal(20, stat.MaxLength);
        }

        // ---------------------------------------------------------------- binding

        private static IFormCollection Form(params (string Key, string Value)[] values) =>
            new FormCollection(values.GroupBy(v => v.Key).ToDictionary(g => g.Key, g => new StringValues(g.Select(v => v.Value).ToArray())));

        private static async Task<EditErrors> BindAsync(EntityEditor editor, BaseEntity entity, IFormCollection form)
        {
            var errors = new EditErrors();
            foreach (var field in editor.Fields)
                await FieldBinder.BindAsync(field, entity, form, errors,
                    field.FixedOptions ?? Array.Empty<FieldOption>(), store: null!, CancellationToken.None);
            return errors;
        }

        [Fact]
        public async Task Texts_are_trimmed_per_language_and_required_in_both()
        {
            var editor = Registry.Find("faq")!.FindList("items")!.Item;
            var item = new FaqItem();
            var errors = await BindAsync(editor, item, Form(
                ("Question.Ar", "  سؤال؟  "), ("Question.En", ""),
                ("Answer.Ar", "سطر\r\nثانٍ"), ("Answer.En", "Line"),
                ("IsOpenByDefault", "false"), ("IsOpenByDefault", "true"), ("IsPublished", "false")));

            Assert.Equal("سؤال؟", item.Question.Ar);
            Assert.Equal("سطر\nثانٍ", item.Answer.Ar);
            Assert.NotNull(errors["Question.En"]);
            Assert.Null(errors["Question.Ar"]);
            Assert.True(item.IsOpenByDefault);   // the checkbox's "true" next to its hidden "false"
            Assert.False(item.IsPublished);
        }

        [Theory]
        [InlineData("#consult", true)]
        [InlineData("https://www.yelgadvisory.com/", true)]
        [InlineData("mailto:info@yelgadvisory.com", true)]
        [InlineData("tel:+201006677363", true)]
        [InlineData("javascript:alert(1)", false)]
        [InlineData("#", false)]
        [InlineData("//evil.example", false)]
        [InlineData("data:text/html,x", false)]
        public void Link_targets_are_anchors_web_mail_or_phone_only(string value, bool valid) =>
            Assert.Equal(valid, FieldBinder.IsHref(value));

        [Fact]
        public async Task A_choice_must_be_one_of_the_options()
        {
            var editor = Registry.Find("industries")!.FindList("categories")!.Item;
            var category = new ClientCategory();
            var errors = await BindAsync(editor, category, Form(
                ("Title.Ar", "بنوك"), ("Title.En", "Banks"), ("Text.Ar", "نص"), ("Text.En", "Text"),
                ("IconKey", "bank"), ("ColorVariant", "purple")));

            Assert.Equal("bank", category.IconKey);
            Assert.NotNull(errors["ColorVariant"]);
        }

        [Fact]
        public async Task Enum_choices_are_bound_by_value()
        {
            var editor = Registry.Find("portal")!.Main!;
            var portal = new ClientPortalSection();
            var errors = await BindAsync(editor, portal, Form(("Title.Ar", "بوابة"), ("Title.En", "Portal"), ("Status", "1")));
            Assert.Equal(PortalStatus.Live, portal.Status);

            // live without an address is refused by the screen's rule
            await editor.ValidateAsync(portal, new EditContext(null!, errors, isNew: false, CancellationToken.None));
            Assert.NotNull(errors[nameof(ClientPortalSection.PortalUrl)]);
        }

        [Fact]
        public void Hints_keep_latin_parts_in_order_and_are_encoded()
        {
            using var writer = new StringWriter();
            AdminFormat.Hint("مثل #consult أو <b>.").WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
            Assert.Equal("مثل <bdi dir=\"ltr\">#consult</bdi> أو &lt;<bdi dir=\"ltr\">b</bdi>&gt;.", writer.ToString());
        }
    }
}
