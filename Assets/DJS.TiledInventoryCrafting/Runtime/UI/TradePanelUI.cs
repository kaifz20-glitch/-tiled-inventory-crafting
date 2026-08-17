using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TiledInventory
{
    /// <summary>
    /// Player-to-player trade UI. Left side: build an offer (offered items + requested
    /// items) and send it. Right side: incoming offers with Accept / Decline.
    /// In single-player the local simulation backend echoes offers back so the flow
    /// is testable offline; in multiplayer offers travel via the network backend.
    /// </summary>
    public class TradePanelUI : MonoBehaviour
    {
        private TradeSystem trading;
        private InventorySystem inventory;
        private RarityPalette palette = new RarityPalette();
        private List<ItemDefinition> knownItems = new List<ItemDefinition>();

        private UIFactory.SelectControl offeredSelect;
        private UIFactory.SelectControl requestedSelect;
        private Text offeredListText;
        private Text requestedListText;
        private RectTransform offersRoot;

        private readonly List<ItemStack> offered = new List<ItemStack>();
        private readonly List<ItemStack> requested = new List<ItemStack>();

        public void Bind(TradeSystem trading, InventorySystem inventory, RarityPalette palette,
            List<ItemDefinition> knownItems,
            RectTransform offerSection, RectTransform offersSection)
        {
            this.trading = trading;
            this.inventory = inventory;
            if (palette != null) this.palette = palette;
            if (knownItems != null) this.knownItems = knownItems;

            trading.OfferCreated += _ => RefreshOffers();
            trading.OfferResolved += _ => RefreshOffers();

            BuildOfferSection(offerSection);
            BuildOffersSection(offersSection);
            RefreshOffers();
        }

        // ------------------------------------------------------------------ create offer

        private void BuildOfferSection(RectTransform section)
        {
            var title = UIFactory.CreateText(section, "Title", "Create Offer", 22, palette.textPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.anchorMin = new Vector2(0f, 0.92f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(10f, 0f);
            title.rectTransform.offsetMax = new Vector2(-10f, 0f);

            // -- offered row
            var offeredRow = UIFactory.CreateRect(section, "OfferedRow", new Vector2(0f, 0.68f), new Vector2(1f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            offeredSelect = UIFactory.CreateSelect(offeredRow, "Item", ItemNames(), 0, null, palette, 16);
            offeredSelect.Root.anchorMin = new Vector2(0f, 0f);
            offeredSelect.Root.anchorMax = new Vector2(0.42f, 1f);
            offeredSelect.Root.offsetMin = new Vector2(4f, 0f);
            offeredSelect.Root.offsetMax = new Vector2(0f, 0f);
            offeredCountLabel = BuildCountEditor(offeredRow, 0.44f, () => offeredCount, v => offeredCount = v);
            var addOffered = UIFactory.CreateButton(offeredRow, "Add", "+", () => AddToOffered(), palette, 18);
            addOffered.GetComponent<RectTransform>().anchorMin = new Vector2(0.72f, 0f);
            addOffered.GetComponent<RectTransform>().anchorMax = new Vector2(0.88f, 1f);
            addOffered.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 2f);
            addOffered.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -2f);

            // -- requested row
            var requestedRow = UIFactory.CreateRect(section, "RequestedRow", new Vector2(0f, 0.44f), new Vector2(1f, 0.66f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            requestedSelect = UIFactory.CreateSelect(requestedRow, "Item", ItemNames(), 0, null, palette, 16);
            requestedSelect.Root.anchorMin = new Vector2(0f, 0f);
            requestedSelect.Root.anchorMax = new Vector2(0.42f, 1f);
            requestedSelect.Root.offsetMin = new Vector2(4f, 0f);
            requestedSelect.Root.offsetMax = new Vector2(0f, 0f);
            requestedCountLabel = BuildCountEditor(requestedRow, 0.44f, () => requestedCount, v => requestedCount = v);
            var addRequested = UIFactory.CreateButton(requestedRow, "Add", "+", () => AddToRequested(), palette, 18);
            addRequested.GetComponent<RectTransform>().anchorMin = new Vector2(0.72f, 0f);
            addRequested.GetComponent<RectTransform>().anchorMax = new Vector2(0.88f, 1f);
            addRequested.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 2f);
            addRequested.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -2f);

            // -- lists + send
            offeredListText = UIFactory.CreateText(section, "OfferedList", "Offering: none", 16, palette.textSecondary, TextAnchor.UpperLeft);
            offeredListText.rectTransform.anchorMin = new Vector2(0f, 0.22f);
            offeredListText.rectTransform.anchorMax = new Vector2(0.5f, 0.44f);
            offeredListText.rectTransform.offsetMin = new Vector2(10f, 0f);
            offeredListText.rectTransform.offsetMax = new Vector2(0f, 0f);
            offeredListText.horizontalOverflow = HorizontalWrapMode.Wrap;

            requestedListText = UIFactory.CreateText(section, "RequestedList", "Requesting: none", 16, palette.textSecondary, TextAnchor.UpperLeft);
            requestedListText.rectTransform.anchorMin = new Vector2(0.5f, 0.22f);
            requestedListText.rectTransform.anchorMax = new Vector2(1f, 0.44f);
            requestedListText.rectTransform.offsetMin = new Vector2(0f, 0f);
            requestedListText.rectTransform.offsetMax = new Vector2(-10f, 0f);
            requestedListText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var sendBtn = UIFactory.CreateButton(section, "Send", "Send Offer", () => SendOffer(), palette, 20);
            sendBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.25f, 0.02f);
            sendBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.75f, 0.2f);
            sendBtn.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 0f);
            sendBtn.GetComponent<RectTransform>().offsetMax = new Vector2(0f, 0f);
        }

        private int offeredCount = 1;
        private int requestedCount = 1;
        private Text offeredCountLabel;
        private Text requestedCountLabel;

        /// <summary>Minus / value / plus editor. Returns the value label.</summary>
        private Text BuildCountEditor(RectTransform row, float x0, System.Func<int> get, System.Action<int> set)
        {
            var minus = UIFactory.CreateButton(row, "Minus", "-", () =>
            {
                set(Mathf.Max(1, get() - 1));
                SyncCountLabels();
            }, palette, 18);
            minus.GetComponent<RectTransform>().anchorMin = new Vector2(x0, 0f);
            minus.GetComponent<RectTransform>().anchorMax = new Vector2(x0 + 0.06f, 1f);

            var value = UIFactory.CreateText(row, "Value", get().ToString(), 18, palette.textPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            value.rectTransform.anchorMin = new Vector2(x0 + 0.07f, 0f);
            value.rectTransform.anchorMax = new Vector2(x0 + 0.15f, 1f);

            var plus = UIFactory.CreateButton(row, "Plus", "+", () =>
            {
                set(get() + 1);
                SyncCountLabels();
            }, palette, 18);
            plus.GetComponent<RectTransform>().anchorMin = new Vector2(x0 + 0.16f, 0f);
            plus.GetComponent<RectTransform>().anchorMax = new Vector2(x0 + 0.22f, 1f);

            return value;
        }

        private void SyncCountLabels()
        {
            if (offeredCountLabel != null) offeredCountLabel.text = offeredCount.ToString();
            if (requestedCountLabel != null) requestedCountLabel.text = requestedCount.ToString();
        }

        private List<string> ItemNames()
        {
            var names = new List<string>();
            foreach (var item in knownItems) names.Add(item.DisplayName);
            return names;
        }

        private ItemDefinition SelectedItem(UIFactory.SelectControl select)
        {
            int idx = select != null ? select.Index : 0;
            return idx >= 0 && idx < knownItems.Count ? knownItems[idx] : null;
        }

        private void AddToOffered()
        {
            var item = SelectedItem(offeredSelect);
            if (item == null) return;
            offered.Add(new ItemStack(item, offeredCount));
            RefreshLists();
        }

        private void AddToRequested()
        {
            var item = SelectedItem(requestedSelect);
            if (item == null) return;
            requested.Add(new ItemStack(item, requestedCount));
            RefreshLists();
        }

        private void RefreshLists()
        {
            offeredListText.text = "Offering: " + Format(offered);
            requestedListText.text = "Requesting: " + Format(requested);
        }

        private static string Format(List<ItemStack> stacks)
        {
            if (stacks.Count == 0) return "none";
            var parts = new List<string>();
            foreach (var s in stacks) parts.Add($"{s.item?.DisplayName} x{s.count}");
            return string.Join(", ", parts);
        }

        private void SendOffer()
        {
            if (trading == null || offered.Count == 0) return;
            trading.CreateOffer(new List<ItemStack>(offered), new List<ItemStack>(requested), "any");
            offered.Clear();
            requested.Clear();
            RefreshLists();
        }

        // ------------------------------------------------------------------ offers list

        private void BuildOffersSection(RectTransform section)
        {
            var title = UIFactory.CreateText(section, "Title", "Pending Offers", 22, palette.textPrimary, TextAnchor.MiddleLeft, FontStyle.Bold);
            title.rectTransform.anchorMin = new Vector2(0f, 0.92f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(10f, 0f);
            title.rectTransform.offsetMax = new Vector2(-10f, 0f);

            var scroll = UIFactory.CreateScrollView(section, "Offers", 6f);
            scroll.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0f);
            scroll.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.9f);
            scroll.GetComponent<RectTransform>().offsetMin = new Vector2(4f, 4f);
            scroll.GetComponent<RectTransform>().offsetMax = new Vector2(-4f, 0f);
            offersRoot = scroll.content;
        }

        private void RefreshOffers()
        {
            if (trading == null || offersRoot == null) return;
            for (int i = offersRoot.childCount - 1; i >= 0; i--)
                Destroy(offersRoot.GetChild(i).gameObject);

            if (trading.Offers.Count == 0)
            {
                var empty = UIFactory.CreateText(offersRoot, "Empty", "No pending offers", 16, palette.textDisabled, TextAnchor.MiddleCenter);
                UIFactory.SetHeight((RectTransform)empty.transform, 40f);
                return;
            }

            foreach (var offer in trading.Offers)
            {
                if (offer.state != TradeState.Pending) continue;
                var row = UIFactory.CreateRect(offersRoot, "Offer", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                UIFactory.SetHeight(row, 56f);
                var img = row.gameObject.AddComponent<Image>();
                img.sprite = UIFactory.GetRoundedFillSprite(6f);
                img.type = Image.Type.Sliced;
                img.color = palette.rowBackground;

                var label = UIFactory.CreateText(row, "Text", $"Offer from {offer.fromPlayerId}\n{Format(offer.offered)}  →  {Format(offer.requested)}", 14, palette.textSecondary, TextAnchor.MiddleLeft);
                label.rectTransform.anchorMin = new Vector2(0f, 0f);
                label.rectTransform.anchorMax = new Vector2(0.66f, 1f);
                label.rectTransform.offsetMin = new Vector2(8f, 2f);
                label.rectTransform.offsetMax = new Vector2(-4f, -2f);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;

                var accept = UIFactory.CreateButton(row, "Accept", "Accept", () => trading.AcceptOffer(offer), palette, 15);
                accept.GetComponent<RectTransform>().anchorMin = new Vector2(0.68f, 0.1f);
                accept.GetComponent<RectTransform>().anchorMax = new Vector2(0.84f, 0.9f);
                var decline = UIFactory.CreateButton(row, "Decline", "Decline", () => trading.DeclineOffer(offer), palette, 15);
                decline.GetComponent<RectTransform>().anchorMin = new Vector2(0.86f, 0.1f);
                decline.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 0.9f);
                decline.GetComponent<RectTransform>().offsetMin = new Vector2(-2f, 0f);
            }
        }
    }
}
