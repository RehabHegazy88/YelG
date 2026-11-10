using System.Text.RegularExpressions;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Media;
using YelG.WebSite.Domain.Sections;
using YelG.WebSite.Domain.Settings;

namespace YelG.WebSite.Areas.Admin.Editing
{
    /// <summary>
    /// Every management screen of the admin panel, in the order of the homepage: which fields each section
    /// and list has, their Arabic labels and the rules a save is checked against.
    /// </summary>
    public static partial class AdminScreens
    {
        private const string ShownHint = "عند الإخفاء يختفي القسم من الموقع ومن روابط القوائم، ويبقى ظاهرًا في المعاينة.";

        private static readonly IReadOnlyList<FieldOption> ServiceIcons = new FieldOption[]
        {
            new("acc", "المحاسبة (acc)"), new("fin", "التحليل المالي (fin)"), new("treas", "الخزينة (treas)"),
            new("bud", "الموازنات (bud)"), new("tax", "الضرائب (tax)"), new("inv", "الاستثمار (inv)"),
            new("erp", "الأنظمة (erp)"), new("audit", "المراجعة (audit)"), new("train", "التدريب (train)")
        };

        private static readonly IReadOnlyList<FieldOption> Colors = new FieldOption[]
        {
            new("blue", "أزرق"), new("coral", "مرجاني"), new("cyan", "سماوي")
        };

        public static EditorRegistry Build()
        {
            var screens = new List<EditorScreen>
            {
                Hero(), Marquee(), About(), Founder(), Services(), Why(), How(), Purpose(), Engage(), Industries(),
                SelfCheck(), Faq(), Consult(), Insights(), Careers(), Portal(), Settings()
            };
            return new EditorRegistry(screens);
        }

        // ---------------------------------------------------------------- sections

        private static EditorScreen Hero()
        {
            var form = new EntityEditor<HeroSection>("الواجهة الرئيسية")
                .Text(x => x.Eyebrow, "العنوان الصغير", hint: "السطر الصغير فوق العنوان الرئيسي.")
                .Area(x => x.Heading, "العنوان الرئيسي", required: true, markup: true, rows: 2)
                .Bool(x => x.HideHeadingBreakOnMobile, "إخفاء السطر الجديد في العنوان على الجوال")
                .Area(x => x.Lead, "النص الداعم", markup: true)
                .Group("الأزرار")
                .Text(x => x.PrimaryCtaLabel, "الزر الأساسي", required: true, hint: "يُستخدم أيضًا لزر الحجز في أعلى الصفحة.")
                .Href(x => x.PrimaryCtaTarget, "وجهة الزر الأساسي", required: true)
                .Text(x => x.SecondaryCtaLabel, "الزر الثانوي")
                .Href(x => x.SecondaryCtaTarget, "وجهة الزر الثانوي")
                .Text(x => x.SelfCheckLinkText, "رابط الفحص المالي", hint: "الرابط المسطّر تحت الأزرار. اتركه فارغًا لإخفائه.")
                .Group("الصورة")
                .Media(x => x.ImageId, "صورة الواجهة")
                .Text(x => x.ImageAlt, "وصف الصورة", hint: "لقارئات الشاشة ومحركات البحث.")
                .Plain(x => x.BadgeRingText, "نص الشارة الدوّارة", hint: "بالأحرف اللاتينية، يظهر كما هو في اللغتين.");

            var stats = new EntityEditor<HeroStat>("رقم")
                .Choice(x => x.Kind, "النوع", new Dictionary<HeroStatKind, string>
                {
                    [HeroStatKind.Number] = "رقم مع وصف",
                    [HeroStatKind.Chips] = "وصف مع رموز"
                })
                .Text(x => x.Value, "الرقم", hint: "للنوع «رقم». يُكتب لكل لغة كما يظهر، مثل +10 بالعربية و10+ بالإنجليزية.")
                .Text(x => x.Caption, "الوصف", required: true)
                .Plain(x => x.ChipCodes, "الرموز", hint: "للنوع «رموز»، مفصولة بفواصل: CMA,CIC,CWM")
                .Text(x => x.ScreenReaderLabel, "الوصف لقارئ الشاشة", hint: "مثل «سنة التأسيس».")
                .Bool(x => x.IsPublished, "ظاهر في الموقع")
                .Rule((s, c) =>
                {
                    if (s.Kind == HeroStatKind.Number)
                    {
                        if (s.Value.Ar.Length == 0) c.Errors.Add("Value.Ar", "مطلوب بالعربية لهذا النوع.");
                        if (s.Value.En.Length == 0) c.Errors.Add("Value.En", "مطلوب بالإنجليزية لهذا النوع.");
                    }
                    else if (string.IsNullOrWhiteSpace(s.ChipCodes)) c.Errors.Add(nameof(s.ChipCodes), "أدخل رمزًا واحدًا على الأقل.");
                })
                .SummaryOf(s => s.Caption.Ar);

            var screen = EditorScreen.For("hero", "الواجهة الرئيسية", form) with { Anchor = "#top", Description = "العنوان والأزرار والصورة والأرقام الثلاثة في أعلى الصفحة." };
            screen.Lists.Add(new ListEditor<HeroStat>("stats", "الأرقام", stats)
                .Show("القيمة", s => s.Kind == HeroStatKind.Number ? s.Value.Ar : s.ChipCodes));
            return screen;
        }

        private static EditorScreen Marquee()
        {
            var form = new EntityEditor<MarqueeSettings>("شريط الخدمات")
                .Bool(x => x.IsPublished, "الشريط ظاهر في الموقع")
                .Area(x => x.ScreenReaderText, "النص لقارئ الشاشة", required: true,
                    hint: "جملة كاملة تُقرأ بدل الشريط المتحرك.");
            var items = new EntityEditor<MarqueeItem>("عنصر")
                .Text(x => x.Label, "النص", required: true)
                .Bool(x => x.IsPublished, "ظاهر في الموقع");

            var screen = EditorScreen.For("marquee", "شريط الخدمات", form) with { Anchor = "#top", Description = "الشريط السماوي المتحرك تحت الواجهة." };
            screen.Lists.Add(new ListEditor<MarqueeItem>("items", "عناصر الشريط", items));
            screen.Actions.Add(new ScreenAction("from-services", "توليد من الخدمات",
                "ستُستبدل عناصر الشريط الحالية بأسماء الخدمات الظاهرة بالترتيب. هل تريد المتابعة؟", GenerateMarqueeAsync));
            return screen;
        }

        private static async Task<string> GenerateMarqueeAsync(IContentStore store, CancellationToken ct)
        {
            var categories = (await store.ListAsync<ServiceCategory>(ct: ct)).Where(c => c.IsPublished).ToList();
            var order = categories.Select((c, i) => (c.Id, i)).ToDictionary(x => x.Id, x => x.i);
            var services = (await store.ListAsync<Service>(s => s.IsPublished, ct))
                .Where(s => order.ContainsKey(s.CategoryId)).OrderBy(s => order[s.CategoryId]).ThenBy(s => s.SortOrder).ThenBy(s => s.Id).ToList();
            if (services.Count == 0) return "لا توجد خدمات ظاهرة لتوليد الشريط منها.";

            foreach (var item in await store.ListForUpdateAsync<MarqueeItem>(ct: ct)) store.Remove(item);
            var sort = 0;
            foreach (var s in services)
                store.Add(new MarqueeItem { Label = new LocalizedText(s.Name.Ar, s.Name.En), SortOrder = sort++ });
            await store.SaveChangesAsync(ct);
            return $"تم توليد الشريط من {services.Count} خدمات.";
        }

        private static EditorScreen About()
        {
            var form = Section<AboutSection>("من نحن", leadLabel: "الفقرة")
                .Text(x => x.CredentialsLabel, "عنوان المؤهلات", hint: "مثل «مؤهلات فريق العمل».")
                .Group("الصور")
                .Media(x => x.Image1Id, "الصورة الكبيرة")
                .Text(x => x.Image1Alt, "وصف الصورة الكبيرة")
                .Media(x => x.Image2Id, "الصورة الصغيرة")
                .Text(x => x.Image2Alt, "وصف الصورة الصغيرة")
                .Plain(x => x.BadgeRingText, "نص الشارة الدوّارة", hint: "بالأحرف اللاتينية، يظهر كما هو في اللغتين.");

            var values = new EntityEditor<AboutValue>("قيمة")
                .Text(x => x.Label, "القيمة", required: true)
                .Bool(x => x.IsPublished, "ظاهرة في الموقع");
            var credentials = new EntityEditor<Credential>("مؤهل")
                .Plain(x => x.Code, "الرمز", required: true, hint: "مثل CMA.")
                .Text(x => x.Description, "الوصف", required: true)
                .Bool(x => x.IsPublished, "ظاهر في الموقع")
                .SummaryOf(c => $"{c.Code} — {c.Description.Ar}");

            var screen = EditorScreen.For("about", "من نحن", form) with { Anchor = "#about", Description = "التعريف والقيم والمؤهلات وصورتا القسم." };
            screen.Lists.Add(new ListEditor<AboutValue>("values", "القيم", values));
            screen.Lists.Add(new ListEditor<Credential>("credentials", "المؤهلات", credentials));
            return screen;
        }

        private static EditorScreen Founder()
        {
            var form = new EntityEditor<FounderSection>("كلمة المؤسس")
                .Bool(x => x.IsPublished, "القسم ظاهر في الموقع", ShownHint)
                .Text(x => x.Eyebrow, "العنوان الصغير")
                .Area(x => x.Quote, "الاقتباس", required: true, markup: true, rows: 4)
                .Area(x => x.SupportingLine, "الفقرة تحت الاقتباس", markup: true)
                .Text(x => x.Name, "الاسم", required: true)
                .Text(x => x.Title, "المنصب", required: true)
                .Group("الصور")
                .Media(x => x.PhotoId, "الصورة الشخصية", hint: "صورة مفرّغة الخلفية (PNG أو WebP) تظهر داخل القوس الملوّن.")
                .Text(x => x.PhotoAlt, "وصف الصورة")
                .Media(x => x.AvatarId, "الصورة الدائرية الصغيرة", hint: "تظهر بجوار الاسم.");
            return EditorScreen.For("founder", "كلمة المؤسس", form) with { Anchor = "#about" };
        }

        private static EditorScreen Services()
        {
            var form = Section<ServicesSection>("الخدمات")
                .Group("نصوص القسم")
                .Text(x => x.AllTabLabel, "تبويب جميع الخدمات", required: true)
                .Text(x => x.CardLinkLabel, "رابط البطاقة", required: true, hint: "مثل «تفاصيل الخدمة».")
                .Text(x => x.ScopeHeading, "عنوان نطاق الخدمة", required: true)
                .Text(x => x.DiscussCtaLabel, "زر مناقشة الخدمة", required: true);

            var categories = new EntityEditor<ServiceCategory>("تصنيف")
                .Plain(x => x.Key, "الرمز", required: true, hint: "حروف إنجليزية صغيرة. يُستخدم في الصفحة؛ لا تغيّره بعد النشر.")
                .Text(x => x.Name, "الاسم", required: true)
                .Bool(x => x.IsPublished, "ظاهر في الموقع", "إخفاء التصنيف يخفي خدماته أيضًا.")
                .Rule(KeyFormat<ServiceCategory>(x => x.Key))
                .Unique(x => x.Key, "هذا الرمز مستخدم لتصنيف آخر.");

            var deliverables = new EntityEditor<ServiceDeliverable>("بند")
                .Text(x => x.Text, "البند", required: true)
                .Bool(x => x.IsPublished, "ظاهر في الموقع");

            var services = new EntityEditor<Service>("خدمة")
                .Text(x => x.Name, "اسم الخدمة", required: true)
                .Reference<ServiceCategory>(x => x.CategoryId, "التصنيف", c => c.Name.Ar)
                .Plain(x => x.Key, "الرمز", required: true,
                    hint: "حروف إنجليزية صغيرة. يربط الخدمة بالنموذج والفحص المالي؛ لا تغيّره بعد النشر.")
                .Choice(x => x.IconKey, "الأيقونة", ServiceIcons)
                .Area(x => x.ShortDescription, "وصف البطاقة", required: true)
                .Area(x => x.LongDescription, "الوصف في التفاصيل", required: true, rows: 5)
                .Bool(x => x.IsPublished, "ظاهرة في الموقع")
                .Rule(KeyFormat<Service>(x => x.Key))
                .Unique(x => x.Key, "هذا الرمز مستخدم لخدمة أخرى.");

            var screen = EditorScreen.For("services", "الخدمات", form) with { Anchor = "#services", Description = "التصنيفات والخدمات ونطاق كل خدمة." };
            screen.Lists.Add(new ListEditor<ServiceCategory>("categories", "التصنيفات", categories)
                .Show("الرمز", c => c.Key));
            screen.Lists.Add(new ListEditor<Service>("items", "الخدمات", services)
                {
                    Description = "تظهر الخدمات في الموقع مجمّعة حسب ترتيب التصنيفات، ثم بالترتيب هنا."
                }
                .Show<ServiceCategory>("التصنيف", s => s.CategoryId, c => c.Name.Ar)
                .Show("الرمز", s => s.Key)
                .Child(new ListEditor<ServiceDeliverable>("deliverables", "نطاق الخدمة", deliverables, d => d.ServiceId)));
            return screen;
        }

        private static EditorScreen Why()
        {
            var form = Section<WhySection>("لماذا YelG")
                .Group("الصورة")
                .Media(x => x.ImageId, "الصورة")
                .Text(x => x.ImageAlt, "وصف الصورة")
                .Area(x => x.FloatingCardText, "نص البطاقة فوق الصورة", markup: true, rows: 2);
            var cards = new EntityEditor<WhyCard>("ميزة")
                .Text(x => x.Title, "العنوان", required: true)
                .Area(x => x.Text, "النص", required: true, markup: true)
                .Bool(x => x.IsHighlighted, "بطاقة مميزة (كحلية)")
                .Bool(x => x.IsPublished, "ظاهرة في الموقع");

            var slides = new EntityEditor<WhySlide>("شريحة")
                .Area(x => x.Title, "العنوان", required: true, markup: true, rows: 2)
                .Area(x => x.Text, "النص", markup: true)
                .Area(x => x.Points, "النقاط", rows: 4, hint: "نقطة في كل سطر. اتركه فارغًا إن لم تكن هناك نقاط.")
                .Media(x => x.ImageId, "الصورة", hint: "صورة بلا كتابة عليها؛ تُقص لتملأ الإطار.")
                .Text(x => x.ImageAlt, "وصف الصورة", hint: "لقارئات الشاشة ومحركات البحث.")
                .Bool(x => x.IsPublished, "ظاهرة في الموقع");

            var screen = EditorScreen.For("why", "لماذا YelG", form) with { Anchor = "#why" };
            screen.Lists.Add(new ListEditor<WhyCard>("cards", "المزايا", cards)
                .Show("مميزة", c => c.IsHighlighted ? "نعم" : ""));
            screen.Lists.Add(new ListEditor<WhySlide>("slides", "الشرائح", slides)
                {
                    Description = "عنوان القسم ومزاياه هما الشريحة الأولى دائمًا، وتأتي هذه الشرائح بعدها بالترتيب. بدون شرائح ظاهرة يظهر القسم بلا تقليب."
                });
            return screen;
        }

        private static EditorScreen How()
        {
            var form = Section<HowSection>("كيف نعمل");
            var steps = new EntityEditor<HowStep>("خطوة")
                .Text(x => x.Title, "العنوان", required: true)
                .Area(x => x.Text, "النص", required: true, markup: true)
                .Bool(x => x.IsHighlighted, "خطوة مميزة (النقطة السماوية)")
                .Bool(x => x.IsPublished, "ظاهرة في الموقع");

            var screen = EditorScreen.For("how", "كيف نعمل", form) with { Anchor = "#how" };
            screen.Lists.Add(new ListEditor<HowStep>("steps", "الخطوات", steps));
            return screen;
        }

        private static EditorScreen Purpose()
        {
            var form = Section<PurposeSection>("الرؤية والمهمة")
                .Group("الرؤية")
                .Text(x => x.VisionTabLabel, "اسم التبويب", required: true)
                .Area(x => x.Vision, "الرؤية", required: true, markup: true)
                .Group("المهمة")
                .Text(x => x.MissionTabLabel, "اسم التبويب", required: true)
                .Area(x => x.Mission, "المهمة", required: true, markup: true)
                .Group("الأهداف")
                .Text(x => x.GoalsTabLabel, "اسم التبويب", required: true, hint: "الأهداف نفسها في القائمة أسفل الصفحة.")
                .Group("الفيديو")
                .Media(x => x.VideoId, "فيديو الهوية", MediaKind.Video)
                .Media(x => x.PosterId, "صورة الغلاف", hint: "تظهر قبل تشغيل الفيديو.")
                .Text(x => x.VideoLabel, "وصف الفيديو");
            var goals = new EntityEditor<PurposeGoal>("هدف")
                .Text(x => x.Label, "الهدف", required: true)
                .Bool(x => x.IsPublished, "ظاهر في الموقع");

            var screen = EditorScreen.For("purpose", "الرؤية والمهمة", form) with { Anchor = "#pp" };
            screen.Lists.Add(new ListEditor<PurposeGoal>("goals", "الأهداف", goals));
            return screen;
        }

        private static EditorScreen Engage()
        {
            var form = Section<EngageSection>("طرق التعاون");
            var bullets = new EntityEditor<EngagementBullet>("نقطة")
                .Text(x => x.Text, "النص", required: true)
                .Bool(x => x.IsPublished, "ظاهرة في الموقع");
            var models = new EntityEditor<EngagementModel>("نموذج تعاون")
                .Text(x => x.Title, "العنوان", required: true)
                .Choice(x => x.IconKey, "الأيقونة", ServiceIcons)
                .Area(x => x.Text, "النص", required: true, markup: true)
                .Text(x => x.CtaLabel, "نص الزر", required: true)
                .Reference<Service>(x => x.PreselectServiceId, "الخدمة المختارة في النموذج", s => s.Name.Ar, emptyLabel: "بدون",
                    hint: "يفتح الزر نموذج الاستشارة وقد اختيرت فيه هذه الخدمة.")
                .Bool(x => x.PreselectNotSure, "اختيار «لست متأكدًا بعد» في النموذج")
                .Bool(x => x.IsHighlighted, "بطاقة مميزة (كحلية)")
                .Bool(x => x.IsPublished, "ظاهر في الموقع")
                .Rule((m, c) =>
                {
                    if (m.PreselectNotSure && m.PreselectServiceId is not null)
                        c.Errors.Add(nameof(m.PreselectNotSure), "اختر خدمة أو «لست متأكدًا بعد»، وليس الاثنين.");
                });

            var screen = EditorScreen.For("engage", "طرق التعاون", form) with { Anchor = "#engage" };
            screen.Lists.Add(new ListEditor<EngagementModel>("models", "نماذج التعاون", models)
                .Child(new ListEditor<EngagementBullet>("bullets", "النقاط", bullets, b => b.EngagementModelId)));
            return screen;
        }

        private static EditorScreen Industries()
        {
            var form = Section<IndustriesSection>("القطاعات والعملاء")
                .Area(x => x.ApprovalNote, "ملاحظة الموافقة", hint: "السطر تحت البطاقات عن نشر أسماء العملاء.");
            var sectors = new EntityEditor<IndustrySector>("قطاع")
                .Text(x => x.Label, "القطاع", required: true)
                .Bool(x => x.IsEmphasized, "مميز (كحلي)")
                .Bool(x => x.IsPublished, "ظاهر في الموقع");
            var categories = new EntityEditor<ClientCategory>("فئة عملاء")
                .Text(x => x.Title, "العنوان", required: true)
                .Area(x => x.Text, "النص", required: true, markup: true)
                .Choice(x => x.IconKey, "الأيقونة", new FieldOption[] { new("bank", "البنوك (bank)"), new("edu", "التعليم (edu)"), new("corp", "الشركات (corp)") })
                .Choice(x => x.ColorVariant, "اللون", Colors)
                .Bool(x => x.IsPublished, "ظاهرة في الموقع");
            var logos = new EntityEditor<ClientLogo>("شعار عميل")
                .Text(x => x.Name, "اسم العميل", required: true)
                .Media(x => x.LogoId, "الشعار", required: true)
                .Url(x => x.WebsiteUrl, "موقع العميل")
                .Bool(x => x.IsApprovedForWeb, "معتمد للنشر",
                    "لا تفعّله إلا بعد موافقة كتابية من العميل. الشعارات غير المعتمدة لا تظهر في الموقع أبدًا، حتى في المعاينة.")
                .Bool(x => x.IsPublished, "ظاهر في الموقع");

            var screen = EditorScreen.For("industries", "القطاعات والعملاء", form) with { Anchor = "#industries" };
            screen.Lists.Add(new ListEditor<IndustrySector>("sectors", "القطاعات", sectors));
            screen.Lists.Add(new ListEditor<ClientCategory>("categories", "فئات العملاء", categories));
            screen.Lists.Add(new ListEditor<ClientLogo>("logos", "شعارات العملاء", logos)
                {
                    Description = "اختيارية. لا يظهر الشعار إلا إذا كان «معتمدًا للنشر»."
                }
                .Show("معتمد", l => l.IsApprovedForWeb ? "معتمد" : "غير معتمد"));
            return screen;
        }

        private static EditorScreen SelfCheck()
        {
            var form = Section<SelfCheckSection>("الفحص المالي")
                .Area(x => x.Disclaimer1, "التنبيه الأول", markup: true, rows: 2)
                .Area(x => x.Disclaimer2, "التنبيه الثاني", markup: true, rows: 2)
                .Group("نصوص الأداة")
                .Text(x => x.PickHint, "إرشاد الاختيار")
                .Text(x => x.BackLabel, "زر الرجوع", required: true)
                .Text(x => x.ResultLabel, "عنوان النتيجة", required: true)
                .Text(x => x.RecommendationsLabel, "عنوان التوصيات", required: true)
                .Area(x => x.MethodNote, "طريقة الحساب", markup: true, rows: 2,
                    hint: "إذا غيّرت عدد الأسئلة أو الدرجات فحدّث الأرقام المكتوبة هنا وفي الفقرة التمهيدية.")
                .Text(x => x.DiscussCtaLabel, "زر مناقشة النتيجة", required: true)
                .Text(x => x.RestartLabel, "زر إعادة الفحص", required: true)
                .Area(x => x.PrefillTemplate, "الرسالة في نموذج الاستشارة", required: true, rows: 2,
                    hint: "{score} النتيجة، و{max} أعلى نتيجة، و{band} اسم المستوى.");

            var questions = new EntityEditor<SelfCheckQuestion>("سؤال الفحص")
                .Text(x => x.Area, "المجال", required: true, hint: "يظهر بجوار رقم السؤال، مثل «السجلات».")
                .Area(x => x.Text, "السؤال", required: true, rows: 2)
                .Reference<Service>(x => x.LinkedServiceId, "الخدمة الموصى بها", s => s.Name.Ar, emptyLabel: "بدون",
                    hint: "تُقترح عندما لا تكون الإجابة أعلى درجة.")
                .Bool(x => x.IsPublished, "ظاهر في الموقع")
                .SummaryOf(q => q.Text.Ar);
            var options = new EntityEditor<SelfCheckOption>("إجابة")
                .Text(x => x.Label, "الإجابة", required: true)
                .Int(x => x.Score, "الدرجة", 0, 100)
                .Bool(x => x.IsPublished, "ظاهرة في الموقع");
            var bands = new EntityEditor<SelfCheckBand>("مستوى")
                .Int(x => x.MinScore, "تبدأ من الدرجة", 0, 10000, hint: "يسري المستوى من هذه الدرجة حتى بداية المستوى التالي.")
                .Text(x => x.Title, "اسم المستوى", required: true)
                .Area(x => x.Text, "النص", required: true, markup: true)
                .Bool(x => x.IsPublished, "ظاهر في الموقع")
                .Rule(async (b, c) =>
                {
                    var id = b.Id;
                    var min = b.MinScore;
                    if (await c.Store.AnyAsync<SelfCheckBand>(x => x.MinScore == min && x.Id != id, c.CancellationToken))
                        c.Errors.Add(nameof(b.MinScore), "يوجد مستوى آخر يبدأ من هذه الدرجة.");
                });

            var screen = EditorScreen.For("selfcheck", "الفحص المالي", form) with
            {
                Anchor = "#health",
                Description = "الدرجة القصوى وعدد الأسئلة يُحسبان من الأسئلة والإجابات تلقائيًا."
            };
            screen.Lists.Add(new ListEditor<SelfCheckQuestion>("questions", "الأسئلة", questions)
                .Show<Service>("الخدمة", q => q.LinkedServiceId, s => s.Name.Ar));
            screen.Lists.Add(new ListEditor<SelfCheckOption>("options", "الإجابات", options)
                {
                    Description = "تظهر لكل سؤال بهذا الترتيب."
                }
                .Show("الدرجة", o => o.Score.ToString()));
            screen.Lists.Add(new ListEditor<SelfCheckBand>("bands", "مستويات النتيجة", bands)
                .Show("من", b => b.MinScore.ToString()));
            return screen;
        }

        private static EditorScreen Faq()
        {
            var form = Section<FaqSection>("الأسئلة الشائعة");
            var items = new EntityEditor<FaqItem>("سؤال")
                .Text(x => x.Question, "السؤال", required: true)
                .Area(x => x.Answer, "الإجابة", required: true, markup: true, rows: 4)
                .Bool(x => x.IsOpenByDefault, "مفتوح عند فتح الصفحة")
                .Bool(x => x.IsPublished, "ظاهر في الموقع");

            var screen = EditorScreen.For("faq", "الأسئلة الشائعة", form) with { Anchor = "#faq" };
            screen.Lists.Add(new ListEditor<FaqItem>("items", "الأسئلة", items));
            return screen;
        }

        private static EditorScreen Consult()
        {
            var form = Section<ConsultSection>("طلب الاستشارة")
                .Text(x => x.DirectContactLabel, "عنوان التواصل المباشر", hint: "يظهر فوق البريد والهاتف من إعدادات الموقع.")
                .Group("النموذج")
                .Text(x => x.FormTitle, "عنوان النموذج", required: true)
                .Text(x => x.RequiredNote, "ملاحظة الحقول الإلزامية", markup: true, hint: @"اكتب \* لإظهار علامة النجمة.")
                .Area(x => x.ConsentText, "نص الموافقة", required: true, markup: true, rows: 2)
                .Text(x => x.NotSureLabel, "خيار «لست متأكدًا بعد»", required: true)
                .Text(x => x.SubmitLabel, "زر الإرسال", required: true)
                .Group("رسائل الإرسال")
                .Text(x => x.SuccessTitle, "عنوان رسالة النجاح", hint: "تظهر بعد حفظ الطلب فقط. تحتاج صياغة معتمدة من الشركة.")
                .Area(x => x.SuccessText, "رسالة النجاح", rows: 2)
                .Area(x => x.FailureText, "رسالة الفشل", rows: 2, hint: "تظهر إذا تعذّر حفظ الطلب.");
            var steps = new EntityEditor<ConsultStep>("خطوة")
                .Text(x => x.Title, "العنوان", required: true)
                .Area(x => x.Text, "النص", required: true, markup: true, rows: 2)
                .Bool(x => x.IsPublished, "ظاهرة في الموقع");

            var screen = EditorScreen.For("consult", "طلب الاستشارة", form) with { Anchor = "#consult" };
            screen.Lists.Add(new ListEditor<ConsultStep>("steps", "خطوات التواصل", steps));
            return screen;
        }

        private static EditorScreen Insights()
        {
            var form = Section<InsightsSection>("المقالات")
                .Text(x => x.FollowLinkLabel, "رابط المتابعة")
                .Url(x => x.FollowLinkUrl, "عنوان رابط المتابعة");
            var posts = new EntityEditor<InsightPost>("خبر أو مقال")
                .Area(x => x.Title, "العنوان", required: true, rows: 2)
                .Plain(x => x.Slug, "عنوان الصفحة", hint: "يظهر في الرابط: /ar/insights/from-insight-to-action. حروف إنجليزية صغيرة وأرقام وشرطات؛ اتركه فارغًا ليُنشأ من العنوان الإنجليزي. لا تغيّره بعد النشر.")
                .Date(x => x.PublishedOn, "تاريخ النشر", hint: "يظهر الشهر والسنة.")
                .Text(x => x.CategoryLabel, "التصنيف", required: true, hint: "مثل «خبر» أو «رؤية» أو «سلسلة».")
                .Choice(x => x.CategoryColor, "لون التصنيف", Colors.Take(2))
                .Group("المحتوى")
                .Area(x => x.Summary, "الملخص", rows: 2, hint: "جملة أو جملتان تظهران تحت العنوان وفي بطاقة الخبر.")
                .Area(x => x.Body, "نص الخبر", markup: true, rows: 14, hint: "افصل الفقرات بسطر فارغ. الفقرة التي تبدأ بـ «## » عنوان فرعي. عندما يكون للخبر نص أو فيديو تكون له صفحة في الموقع؛ وبدونهما تفتح بطاقته الرابط الخارجي.")
                .Media(x => x.ImageId, "الصورة", hint: "تظهر في البطاقة وأعلى صفحة الخبر، وغلافًا للفيديو.")
                .Text(x => x.ImageAlt, "وصف الصورة")
                .Media(x => x.VideoId, "الفيديو (اختياري)", MediaKind.Video, hint: "يظهر أعلى صفحة الخبر بدل الصورة. ارفعه أولًا في مكتبة الوسائط (MP4 أو WebM).")
                .Url(x => x.YouTubeUrl, "أو رابط فيديو YouTube", hint: "مثل https://www.youtube.com/watch?v=… أو https://youtu.be/… — يُشغَّل داخل صفحة الخبر، ولا يُحمَّل من YouTube إلا عندما يضغط الزائر تشغيل.")
                .Group("النشر الخارجي")
                .Url(x => x.ExternalUrl, "رابط الخبر على LinkedIn أو غيره", hint: "اختياري عندما يكون للخبر نص في الموقع؛ يظهر رابطًا «اقرأه على …».")
                .Plain(x => x.Source, "المصدر", hint: "مثل LinkedIn.")
                .Bool(x => x.IsPublished, "ظاهر في الموقع")
                .Rule((p, c) =>
                {
                    if (string.IsNullOrWhiteSpace(p.Slug)) p.Slug = Slugify(p.Title.En);
                    if (string.IsNullOrWhiteSpace(p.Slug)) c.Errors.Add(nameof(p.Slug), "أدخل عنوانًا للصفحة بالحروف الإنجليزية.");
                    else if (!SlugPattern().IsMatch(p.Slug)) c.Errors.Add(nameof(p.Slug), "حروف إنجليزية صغيرة وأرقام وشرطات فقط، مثل from-insight-to-action.");
                    if (!string.IsNullOrWhiteSpace(p.YouTubeUrl) && p.YouTubeId is null)
                        c.Errors.Add(nameof(p.YouTubeUrl), "هذا ليس رابط فيديو على YouTube. انسخ الرابط من زر «مشاركة» تحت الفيديو.");
                    else if (p.YouTubeId is not null && p.VideoId is not null)
                        c.Errors.Add(nameof(p.YouTubeUrl), "اختر فيديو واحدًا: المرفوع أو رابط YouTube.");
                    if (p.Body.IsEmpty && !p.HasVideo && string.IsNullOrWhiteSpace(p.ExternalUrl) && string.IsNullOrWhiteSpace(p.YouTubeUrl))
                        c.Errors.Add(nameof(p.Body), "اكتب نص الخبر، أو أضف فيديو، أو رابطًا خارجيًا.");
                })
                .Unique(x => x.Slug, "هذا العنوان مستخدم لخبر آخر.");

            var screen = EditorScreen.For("insights", "الأخبار والمقالات", form) with
            {
                Anchor = "#insights",
                Description = "قسم المقالات في الصفحة الرئيسية، وصفحة الأخبار والمقالات (/ar/insights)، وصفحة كل خبر له نص أو فيديو."
            };
            screen.Lists.Add(new ListEditor<InsightPost>("posts", "المقالات", posts)
                .Show("التاريخ", p => p.PublishedOn.ToString("yyyy-MM")));
            return screen;
        }

        private static EditorScreen Careers()
        {
            var form = Section<CareersSection>("الوظائف")
                .Group("الصورة")
                .Media(x => x.ImageId, "الصورة")
                .Text(x => x.ImageAlt, "وصف الصورة")
                .Group("الروابط")
                .Text(x => x.SendCvLabel, "زر إرسال السيرة الذاتية", hint: "يفتح نموذج التقديم العام في صفحة الوظائف.")
                .Plain(x => x.CvEmailSubject, "عنوان الرسالة", ltr: false)
                .Text(x => x.JobsLinkLabel, "رابط الوظائف")
                .Url(x => x.JobsUrl, "عنوان رابط الوظائف")
                .Group("صفحة الوظائف ونموذج التقديم")
                .Text(x => x.PageHeading, "عنوان صفحة الوظائف", hint: "اتركه فارغًا للنص الافتراضي. عنوان القسم أعلاه يظهر في الصفحة الرئيسية فقط.")
                .Area(x => x.PageLead, "مقدمة صفحة الوظائف", rows: 3, hint: "اتركها فارغة للنص الافتراضي.")
                .Text(x => x.ApplyTitle, "عنوان نموذج التقديم", hint: "اتركه فارغًا لـ «قدّم على هذه الوظيفة».")
                .Area(x => x.ApplyConsentText, "نص الموافقة", markup: true, rows: 2, hint: "ما يوافق عليه المتقدم (استخدام سيرته ومدة الاحتفاظ بها). تحتاج صياغة معتمدة.")
                .Text(x => x.ApplySuccessTitle, "عنوان رسالة النجاح")
                .Area(x => x.ApplySuccessText, "رسالة النجاح", rows: 2)
                .Area(x => x.NoOpeningsText, "نص «لا توجد وظائف»", rows: 2, hint: "يظهر في صفحة الوظائف عندما لا توجد وظيفة متاحة.");
            var tags = new EntityEditor<CareerRoleTag>("وسم")
                .Text(x => x.Label, "الوسم", required: true)
                .Bool(x => x.IsPublished, "ظاهر في الموقع");
            var questions = new EntityEditor<CareerRoleQuestion>("سؤال")
                .Area(x => x.Text, "السؤال", required: true, rows: 2)
                .Choice(x => x.Kind, "نوع الإجابة", new Dictionary<QuestionKind, string>
                {
                    [QuestionKind.YesNo] = "نعم / لا",
                    [QuestionKind.ShortText] = "نص قصير (سطر واحد)",
                    [QuestionKind.LongText] = "نص طويل (عدة أسطر)",
                    [QuestionKind.SingleChoice] = "اختيار واحد من قائمة",
                    [QuestionKind.MultipleChoice] = "اختيار متعدد من قائمة"
                })
                .Area(x => x.Options, "الاختيارات", rows: 5,
                    hint: "لأسئلة الاختيار فقط: كل اختيار في سطر، وبالترتيب نفسه في العربية والإنجليزية. لا تغيّر ترتيبها بعد وصول طلبات.")
                .Text(x => x.Hint, "توضيح تحت السؤال", hint: "اختياري.")
                .Bool(x => x.IsRequired, "إجابة مطلوبة")
                .Bool(x => x.IsPublished, "ظاهر في النموذج")
                .Rule((q, c) =>
                {
                    if (!q.HasOptions) return;
                    static int Count(string? s) => (s ?? "").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length;
                    var ar = Count(q.Options.Ar);
                    var en = Count(q.Options.En);
                    if (ar < 2) c.Errors.Add(nameof(q.Options), "أدخل اختيارين على الأقل، كل اختيار في سطر.");
                    else if (en > 0 && en != ar) c.Errors.Add(nameof(q.Options), $"عدد الاختيارات بالإنجليزية ({en}) لا يساوي العربية ({ar}).");
                })
                .SummaryOf(q => q.Text.Ar);
            var roles = new EntityEditor<CareerRole>("وظيفة")
                .Text(x => x.Title, "المسمى الوظيفي", required: true)
                .Plain(x => x.Slug, "عنوان الصفحة", hint: "يظهر في الرابط: /ar/careers/senior-accountant. حروف إنجليزية صغيرة وأرقام وشرطات؛ اتركه فارغًا ليُنشأ من المسمى الإنجليزي. لا تغيّره بعد النشر.")
                .Text(x => x.Department, "القسم", hint: "مثل «المحاسبة» — تُصفّى الوظائف به في صفحة الوظائف.")
                .Choice(x => x.EmploymentType, "نوع الدوام", new Dictionary<EmploymentType, string>
                {
                    [EmploymentType.FullTime] = "دوام كامل",
                    [EmploymentType.PartTime] = "دوام جزئي",
                    [EmploymentType.Contract] = "عقد مؤقت",
                    [EmploymentType.Internship] = "تدريب"
                })
                .Text(x => x.Location, "المكان")
                .Text(x => x.Experience, "الخبرة")
                .Group("الوصف")
                .Area(x => x.Summary, "ملخص قصير", rows: 2, hint: "سطر أو سطران يظهران في قائمة الوظائف.")
                .Area(x => x.Description, "عن الوظيفة", markup: true, rows: 5)
                .Area(x => x.Responsibilities, "المسؤوليات", rows: 6, hint: "كل مسؤولية في سطر.")
                .Area(x => x.Requirements, "المتطلبات", rows: 6, hint: "كل متطلب في سطر.")
                .Group("النشر والتقديم")
                .OptionalDate(x => x.PostedOn, "تاريخ النشر")
                .OptionalDate(x => x.ClosingOn, "آخر موعد للتقديم", hint: "بعد هذا اليوم تختفي الوظيفة من الموقع تلقائيًا. اتركه فارغًا لوظيفة مفتوحة.")
                .Bool(x => x.IsActive, "متاحة للتقديم", "الوظيفة غير المتاحة تبقى محفوظة ولا تظهر في الموقع.")
                .Bool(x => x.IsPublished, "ظاهرة في الموقع")
                .Url(x => x.ApplyUrl, "رابط تقديم خارجي", hint: "اختياري. إذا أدخلته يظهر زر يفتح هذا الرابط بدل نموذج التقديم في الموقع.")
                .Email(x => x.ApplyEmail, "بريد التقديم (قديم)", hint: "اختياري؛ الطلبات تصل عبر النموذج إلى «طلبات التوظيف».")
                .Rule((r, c) =>
                {
                    if (string.IsNullOrWhiteSpace(r.Slug)) r.Slug = Slugify(r.Title.En);
                    if (string.IsNullOrWhiteSpace(r.Slug)) c.Errors.Add(nameof(r.Slug), "أدخل عنوانًا للصفحة بالحروف الإنجليزية.");
                    else if (!SlugPattern().IsMatch(r.Slug)) c.Errors.Add(nameof(r.Slug), "حروف إنجليزية صغيرة وأرقام وشرطات فقط، مثل senior-accountant.");
                    if (r.PostedOn is { } posted && r.ClosingOn is { } closing && closing < posted)
                        c.Errors.Add(nameof(r.ClosingOn), "آخر موعد للتقديم قبل تاريخ النشر.");
                })
                .Unique(x => x.Slug, "هذا العنوان مستخدم لوظيفة أخرى.");

            var screen = EditorScreen.For("careers", "الوظائف", form) with
            {
                Anchor = "#careers",
                Description = "قسم الوظائف في الصفحة الرئيسية وصفحة الوظائف (/ar/careers) وصفحة كل وظيفة. طلبات التقديم في «طلبات التوظيف»."
            };
            screen.Lists.Add(new ListEditor<CareerRole>("roles", "الوظائف", roles)
                .Show("القسم", r => r.Department.Ar)
                .Show("الحالة", r => !r.IsActive ? "مغلقة" : r.ClosingOn is { } d && d < DateOnly.FromDateTime(DateTime.UtcNow) ? "انتهى موعدها" : "متاحة")
                .Child(new ListEditor<CareerRoleTag>("tags", "الوسوم", tags, t => t.CareerRoleId))
                .Child(new ListEditor<CareerRoleQuestion>("questions", "أسئلة إضافية في نموذج التقديم", questions, q => q.CareerRoleId)
                    {
                        Description = "تظهر بعد السيرة الذاتية في نموذج هذه الوظيفة، ويمكن تصفية الطلبات بإجاباتها في «طلبات التوظيف»."
                    }
                    .Show("النوع", q => q.Kind switch
                    {
                        QuestionKind.YesNo => "نعم / لا",
                        QuestionKind.ShortText => "نص قصير",
                        QuestionKind.LongText => "نص طويل",
                        QuestionKind.SingleChoice => "اختيار واحد",
                        _ => "اختيار متعدد"
                    })
                    .Show("مطلوب", q => q.IsRequired ? "نعم" : "لا")));
            return screen;
        }

        private static EditorScreen Portal()
        {
            var form = new EntityEditor<ClientPortalSection>("بوابة العملاء")
                .Bool(x => x.IsPublished, "القسم ظاهر في الموقع", ShownHint)
                .Choice(x => x.Status, "الحالة", new Dictionary<PortalStatus, string>
                {
                    [PortalStatus.ComingSoon] = "قريبًا",
                    [PortalStatus.Live] = "متاحة"
                }, hint: "لا يظهر زر الدخول إلا عندما تكون البوابة متاحة ولها رابط.")
                .Text(x => x.BadgeLabel, "شارة «قريبًا»")
                .Text(x => x.Title, "العنوان", required: true)
                .Area(x => x.Text, "النص", markup: true)
                .Text(x => x.SignInLabel, "زر الدخول")
                .Url(x => x.PortalUrl, "رابط البوابة")
                .Rule((p, c) =>
                {
                    if (p.Status != PortalStatus.Live) return;
                    if (string.IsNullOrWhiteSpace(p.PortalUrl)) c.Errors.Add(nameof(p.PortalUrl), "أدخل رابط البوابة لتفعيلها.");
                    if (p.SignInLabel.Ar.Length == 0) c.Errors.Add("SignInLabel.Ar", "مطلوب بالعربية عندما تكون البوابة متاحة.");
                    if (p.SignInLabel.En.Length == 0) c.Errors.Add("SignInLabel.En", "مطلوب بالإنجليزية عندما تكون البوابة متاحة.");
                });
            return EditorScreen.For("portal", "بوابة العملاء", form) with { Anchor = "#portal" };
        }

        private static EditorScreen Settings()
        {
            var form = new EntityEditor<SiteSettings>("إعدادات الموقع")
                .Group("الهوية")
                .Media(x => x.LogoId, "الشعار", hint: "اتركه فارغًا لعرض الشعار المصمم الحالي.")
                .Text(x => x.Tagline, "سطر التعريف في التذييل")
                .Text(x => x.Copyright, "حقوق النشر", hint: "{year} تُستبدل بالسنة الحالية.")
                .Group("التواصل")
                .Email(x => x.ContactEmail, "البريد المعروض في الموقع", required: true)
                .Email(x => x.NotificationEmail, "بريد استلام الطلبات", required: true, hint: "تُرسل إليه طلبات الاستشارة الجديدة.")
                .Plain(x => x.Phone, "الهاتف", hint: "كما يظهر، مثل +20 10 0667 7363.")
                .Url(x => x.WebsiteUrl, "الموقع الإلكتروني")
                .Url(x => x.LinkedInUrl, "صفحة لينكدإن")
                .Email(x => x.CareersEmail, "بريد التوظيف")
                .Group("اللغة ومحركات البحث")
                .Choice(x => x.DefaultLanguage, "اللغة الافتراضية", new FieldOption[] { new("ar", "العربية"), new("en", "الإنجليزية") },
                    hint: "اللغة التي يفتح بها عنوان الموقع الرئيسي.")
                .Text(x => x.SeoTitle, "عنوان الصفحة", required: true)
                .Area(x => x.SeoDescription, "وصف الصفحة", required: true, rows: 2)
                .Media(x => x.OgImageId, "صورة المشاركة", hint: "تظهر عند مشاركة الرابط. بدونها تُستخدم صورة الواجهة.")
                .Rule((s, c) =>
                {
                    if (s.Phone.Length > 0 && !PhonePattern().IsMatch(s.Phone)) c.Errors.Add(nameof(s.Phone), "أدخل رقمًا مثل +20 10 0667 7363.");
                });
            return EditorScreen.For("settings", "إعدادات الموقع", form) with { AdminOnly = true, Description = "بيانات التواصل والهوية ومحركات البحث." };
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>The fields every section shares.</summary>
        private static EntityEditor<T> Section<T>(string title, string leadLabel = "الفقرة التمهيدية") where T : SectionBase, new() =>
            new EntityEditor<T>(title)
                .Bool(x => x.IsPublished, "القسم ظاهر في الموقع", ShownHint)
                .Text(x => x.Eyebrow, "العنوان الصغير", hint: "السطر الصغير فوق العنوان.")
                .Area(x => x.Heading, "العنوان", required: true, markup: true, rows: 2)
                .Bool(x => x.HideHeadingBreakOnMobile, "إخفاء السطر الجديد في العنوان على الجوال")
                .Area(x => x.Lead, leadLabel, markup: true);

        private static Action<T, EditContext> KeyFormat<T>(Func<T, string> key) => (e, c) =>
        {
            if (key(e).Length > 0 && !KeyPattern().IsMatch(key(e)))
                c.Errors.Add("Key", "حروف إنجليزية صغيرة وأرقام وشرطة فقط، تبدأ بحرف.");
        };

        /// <summary>"Senior Accountant (Cairo)" -> "senior-accountant-cairo".</summary>
        private static string Slugify(string text)
        {
            var slug = SlugCleanup().Replace(text.ToLowerInvariant(), "-").Trim('-');
            return slug.Length > 80 ? slug[..80].TrimEnd('-') : slug;
        }

        [GeneratedRegex("[^a-z0-9]+")]
        private static partial Regex SlugCleanup();

        [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
        private static partial Regex SlugPattern();

        [GeneratedRegex("^[a-z][a-z0-9-]{0,19}$")]
        private static partial Regex KeyPattern();

        [GeneratedRegex(@"^\+?[0-9][0-9 ]{4,24}$")]
        private static partial Regex PhonePattern();
    }
}
