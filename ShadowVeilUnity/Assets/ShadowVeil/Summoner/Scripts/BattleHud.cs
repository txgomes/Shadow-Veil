using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowVeil.Summoner
{
    /// <summary>
    /// Battle HUD laid out like First Summoner: currency-style top bar with timer and objective diamonds,
    /// a vertical lane tracker on the right, and the mana bar over six cards at the bottom.
    /// </summary>
    public sealed class BattleHud
    {
        private sealed class CardSlot
        {
            public CardDefinition card;
            public Button button;
            public Image face;
            public Image cooldownShade;
            public Text cooldownText;
            public Text costText;
        }

        private readonly BattleController battle;
        private readonly Canvas canvas;
        private readonly List<CardSlot> slots = new List<CardSlot>();
        private readonly Image[] objectiveGems = new Image[3];
        private Text timerText;
        private Text manaText;
        private Image manaFill;
        private Image heroFill;
        private Text heroText;
        private Image enemyPedestalFill;
        private Image playerPedestalFill;
        private RectTransform trackHero;
        private Text toastText;
        private float toastTime;
        private GameObject overlay;

        public BattleHud(BattleController battle)
        {
            this.battle = battle;
            canvas = Ui.CreateCanvas("Battle HUD");
            BuildTopBar();
            BuildTracker();
            BuildCardDock();
            toastText = Ui.Label("Toast", canvas.transform, "", 34, TextAnchor.MiddleCenter, Ui.Gold, new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.70f));
        }

        private void BuildTopBar()
        {
            Transform root = canvas.transform;
            Ui.Framed("Top bar", root, new Vector2(0f, 1f), new Vector2(1f, 1f), Ui.Panel, new Vector2(-4f, -150f), new Vector2(4f, 4f));
            timerText = Ui.Label("Timer", root, "05:00", 40, TextAnchor.MiddleCenter, Color.white, new Vector2(0.38f, 0.948f), new Vector2(0.62f, 0.99f));
            for (int i = 0; i < 3; i++)
            {
                RectTransform gem = Ui.Area("Objective " + (i + 1), root, new Vector2(0.415f + i * 0.06f, 0.928f), new Vector2(0.455f + i * 0.06f, 0.948f));
                gem.localRotation = Quaternion.Euler(0f, 0f, 45f);
                Image image = gem.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                objectiveGems[i] = image;
            }
            Ui.Label("Enemy label", root, "ENEMY PEDESTAL", 18, TextAnchor.MiddleRight, Ui.Parchment, new Vector2(0.62f, 0.965f), new Vector2(0.97f, 0.99f));
            enemyPedestalFill = Ui.Bar("Enemy pedestal", root, new Vector2(0.64f, 0.950f), new Vector2(0.97f, 0.962f), new Color(0.85f, 0.2f, 0.18f));
            Ui.Label("Player label", root, "YOUR PEDESTAL", 18, TextAnchor.MiddleLeft, Ui.Parchment, new Vector2(0.03f, 0.965f), new Vector2(0.38f, 0.99f));
            playerPedestalFill = Ui.Bar("Player pedestal", root, new Vector2(0.03f, 0.950f), new Vector2(0.36f, 0.962f), new Color(0.3f, 0.65f, 0.95f));
            heroText = Ui.Label("Hero label", root, "RACHEL", 18, TextAnchor.MiddleLeft, Ui.Parchment, new Vector2(0.03f, 0.918f), new Vector2(0.36f, 0.944f));
            heroFill = Ui.Bar("Hero health", root, new Vector2(0.03f, 0.905f), new Vector2(0.36f, 0.915f), new Color(0.35f, 0.85f, 0.4f));
        }

        private void BuildTracker()
        {
            Transform root = canvas.transform;
            Image track = Ui.Box("Lane tracker", root, new Vector2(0.955f, 0.30f), new Vector2(0.965f, 0.86f), new Color(0.05f, 0.04f, 0.05f, 0.85f));
            Marker(track.transform, "Enemy pedestal marker", 1f, new Color(0.95f, 0.25f, 0.2f));
            Marker(track.transform, "Gem marker", Mathf.InverseLerp(Arena.PlayerPedestalZ, Arena.EnemyPedestalZ, Arena.GemZ), new Color(0.3f, 0.6f, 1f));
            Marker(track.transform, "Your pedestal marker", 0f, new Color(0.35f, 0.75f, 0.95f));
            trackHero = Marker(track.transform, "Hero marker", 0f, Ui.Gold);
            trackHero.sizeDelta = new Vector2(26f, 26f);
        }

        private static RectTransform Marker(Transform track, string name, float t, Color color)
        {
            RectTransform rect = Ui.Area(name, track, new Vector2(0.5f, t), new Vector2(0.5f, t));
            rect.sizeDelta = new Vector2(20f, 20f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private void BuildCardDock()
        {
            Transform root = canvas.transform;
            Ui.Framed("Card dock", root, new Vector2(0f, 0f), new Vector2(1f, 0f), Ui.Panel, new Vector2(-4f, -4f), new Vector2(4f, 250f));
            manaFill = Ui.Bar("Mana", root, new Vector2(0.03f, 0.175f), new Vector2(0.97f, 0.19f), Ui.ManaPurple);
            manaText = Ui.Label("Mana value", root, "0", 26, TextAnchor.MiddleLeft, new Color(0.85f, 0.7f, 1f), new Vector2(0.03f, 0.153f), new Vector2(0.5f, 0.175f));

            IReadOnlyList<CardDefinition> deck = battle.Deck;
            float width = 0.15f, gap = 0.012f;
            float left = (1f - (width * deck.Count + gap * (deck.Count - 1))) * 0.5f;
            for (int i = 0; i < deck.Count; i++)
            {
                CardDefinition card = deck[i];
                float x = left + i * (width + gap);
                Image frame = Ui.Box("Card " + card.title, root, new Vector2(x, 0.015f), new Vector2(x + width, 0.145f), CardLibrary.RarityColors[(int)card.rarity]);
                frame.raycastTarget = true;
                Button button = frame.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                int index = i;
                button.onClick.AddListener(() => battle.PlayCard(index));
                Image face = Ui.Box("Face", frame.transform, Vector2.zero, Vector2.one, card.tint * 0.55f + new Color(0f, 0f, 0f, 0.45f), new Vector2(4f, 4f), new Vector2(-4f, -4f));
                Image portrait = Ui.Box("Portrait", face.transform, new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.92f), card.tint);
                // Optional local art: drop sprites in a Resources/CardArt folder named after the card id.
                Sprite art = Resources.Load<Sprite>("CardArt/" + card.id);
                if (art != null) { portrait.sprite = art; portrait.color = Color.white; portrait.preserveAspect = true; }
                Ui.Label("Title", face.transform, card.title, 15, TextAnchor.LowerCenter, Color.white, new Vector2(0f, 0.02f), new Vector2(1f, 0.4f));
                Image badge = Ui.Box("Cost badge", frame.transform, new Vector2(-0.08f, 0.78f), new Vector2(0.32f, 1.05f), new Color(0.25f, 0.10f, 0.40f));
                Text cost = Ui.Label("Cost", badge.transform, card.mana.ToString(), 24, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one);
                Image shade = Ui.Box("Cooldown", frame.transform, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.72f));
                shade.type = Image.Type.Filled;
                shade.fillMethod = Image.FillMethod.Radial360;
                shade.fillOrigin = (int)Image.Origin360.Top;
                shade.fillClockwise = false;
                shade.sprite = Ui.WhiteSprite();
                Text cooldownText = Ui.Label("Cooldown text", frame.transform, "", 28, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one);
                slots.Add(new CardSlot { card = card, button = button, face = face, cooldownShade = shade, cooldownText = cooldownText, costText = cost });
            }
        }

        public void Toast(string message)
        {
            toastText.text = message;
            toastTime = 2.2f;
        }

        public void Refresh(float dt)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(battle.TimeLeft));
            timerText.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            timerText.color = seconds <= 30 ? new Color(1f, 0.4f, 0.35f) : Color.white;
            for (int i = 0; i < 3; i++)
                objectiveGems[i].color = battle.ObjectiveOnTrack(i) ? new Color(1f, 0.82f, 0.25f) : new Color(0.25f, 0.22f, 0.2f);

            manaFill.fillAmount = battle.Mana / battle.MaxMana;
            manaText.text = Mathf.FloorToInt(battle.Mana) + " / " + Mathf.FloorToInt(battle.MaxMana);
            Unit hero = battle.Hero;
            heroFill.fillAmount = hero != null ? hero.hp / hero.maxHp : 0f;
            heroText.text = hero != null && !hero.Alive ? "RACHEL  —  REVIVING " + Mathf.CeilToInt(battle.HeroReviveIn) : "RACHEL";
            enemyPedestalFill.fillAmount = battle.EnemyPedestal != null ? battle.EnemyPedestal.hp / battle.EnemyPedestal.maxHp : 0f;
            playerPedestalFill.fillAmount = battle.PlayerPedestal != null ? battle.PlayerPedestal.hp / battle.PlayerPedestal.maxHp : 0f;
            if (hero != null)
            {
                float t = Mathf.Clamp01(Mathf.InverseLerp(Arena.PlayerPedestalZ, Arena.EnemyPedestalZ, hero.Position.z));
                trackHero.anchorMin = trackHero.anchorMax = new Vector2(0.5f, t);
            }

            for (int i = 0; i < slots.Count; i++)
            {
                CardSlot slot = slots[i];
                float remaining = battle.CooldownLeft(i);
                bool affordable = battle.Mana >= slot.card.mana;
                slot.cooldownShade.fillAmount = remaining > 0f ? remaining / slot.card.cooldown : 0f;
                slot.cooldownText.text = remaining > 0f ? Mathf.CeilToInt(remaining).ToString() : "";
                slot.button.interactable = remaining <= 0f && affordable && !battle.Ended;
                slot.costText.color = affordable ? Color.white : new Color(1f, 0.45f, 0.45f);
                slot.face.color = slot.card.tint * (affordable ? 0.55f : 0.3f) + new Color(0f, 0f, 0f, 0.45f);
            }

            if (toastTime > 0f)
            {
                toastTime -= dt;
                Color c = toastText.color;
                c.a = Mathf.Clamp01(toastTime / 0.5f);
                toastText.color = c;
            }
        }

        public void ShowIntro(string stage, string[] objectives, UnityEngine.Events.UnityAction onStart)
        {
            overlay = Ui.Box("Intro shade", canvas.transform, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.75f)).gameObject;
            overlay.GetComponent<Image>().raycastTarget = true;
            Image panel = Ui.Framed("Intro panel", overlay.transform, new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.72f), Ui.Panel);
            Ui.Label("Stage", panel.transform, stage, 44, TextAnchor.MiddleCenter, Ui.Gold, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f));
            for (int i = 0; i < objectives.Length; i++)
                Ui.Label("Objective " + i, panel.transform, "•  " + objectives[i], 26, TextAnchor.MiddleLeft, Ui.Parchment, new Vector2(0.1f, 0.6f - i * 0.12f), new Vector2(0.95f, 0.7f - i * 0.12f));
            Ui.Label("Hint", panel.transform, "Drag to move Rachel · tap an enemy to focus it · tap cards to summon", 20, TextAnchor.MiddleCenter, new Color(0.7f, 0.65f, 0.58f), new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.3f));
            Ui.ActionButton("Start", panel.transform, "ENTER", new Vector2(0.3f, 0.04f), new Vector2(0.7f, 0.17f), () =>
            {
                Object.Destroy(overlay);
                onStart();
            });
        }

        public void ShowResult(bool won, bool[] stars, string[] objectives, int gold, UnityEngine.Events.UnityAction onRetry)
        {
            overlay = Ui.Box("Result shade", canvas.transform, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.78f)).gameObject;
            overlay.GetComponent<Image>().raycastTarget = true;
            Image panel = Ui.Framed("Result panel", overlay.transform, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.74f), Ui.Panel);
            Ui.Label("Title", panel.transform, won ? "VICTORY" : "DEFEAT", 64, TextAnchor.MiddleCenter, won ? Ui.Gold : new Color(0.85f, 0.3f, 0.25f), new Vector2(0f, 0.8f), new Vector2(1f, 0.97f));
            for (int i = 0; i < 3; i++)
            {
                RectTransform gem = Ui.Area("Star " + i, panel.transform, new Vector2(0.32f + i * 0.18f, 0.7f), new Vector2(0.32f + i * 0.18f, 0.7f));
                gem.sizeDelta = new Vector2(54f, 54f);
                gem.localRotation = Quaternion.Euler(0f, 0f, 45f);
                Image image = gem.gameObject.AddComponent<Image>();
                image.color = stars[i] ? new Color(1f, 0.82f, 0.25f) : new Color(0.22f, 0.2f, 0.18f);
                Ui.Label("Objective " + i, panel.transform, objectives[i], 24, TextAnchor.MiddleLeft, stars[i] ? Ui.Parchment : new Color(0.55f, 0.5f, 0.45f),
                    new Vector2(0.1f, 0.48f - i * 0.09f), new Vector2(0.95f, 0.56f - i * 0.09f));
            }
            Ui.Label("Reward", panel.transform, won ? "Reward:  " + gold + " gold" : "Rachel retreats to Renommus Village...", 26, TextAnchor.MiddleCenter, Ui.Gold, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.28f));
            Ui.ActionButton("Retry", panel.transform, won ? "PLAY AGAIN" : "RETRY", new Vector2(0.28f, 0.04f), new Vector2(0.72f, 0.16f), onRetry);
        }
    }
}
