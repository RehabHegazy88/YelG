namespace YelG.WebSite.Domain.Common
{
    /// <summary>
    /// Icons the design draws in CSS (masks), so editors pick from these lists instead of uploading icons.
    /// </summary>
    public static class IconKeys
    {
        /// <summary>Service icons: CSS class "ic-{key}".</summary>
        public static readonly IReadOnlyList<string> Service = new[] { "acc", "fin", "treas", "bud", "tax", "inv", "erp", "audit", "train" };

        /// <summary>Client category icons in the Industries section.</summary>
        public static readonly IReadOnlyList<string> ClientCategory = new[] { "bank", "edu", "corp" };

        /// <summary>Tint of a client category icon tile.</summary>
        public static readonly IReadOnlyList<string> ClientCategoryColor = new[] { "blue", "coral", "cyan" };

        /// <summary>Colour of an insight post's category line.</summary>
        public static readonly IReadOnlyList<string> InsightColor = new[] { "blue", "coral" };
    }
}
