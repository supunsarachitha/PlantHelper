namespace PlantHelper.Theme;

/// <summary>Rotating "tip of the day" shown on the dashboard.</summary>
public static class GrowerTips
{
    private static readonly (string Title, string Text)[] Tips =
    [
        ("Water deeply, not often", "Soak until 10–20% runs off, then let the top few centimetres dry. Frequent sips keep roots shallow."),
        ("Check pH every time", "Most nutrients lock out outside pH 5.5–6.5 (hydro/coco) or 6.0–7.0 (soil). Log pH with each feed to spot drift early."),
        ("Lift the pot", "A dry pot is surprisingly light. Lifting containers is the quickest way to know if a plant really needs water."),
        ("Feed at half strength first", "Start new nutrients at 50% of the label dose and watch the leaf tips. Burnt tips mean it's too strong."),
        ("Mind the VPD", "Aim for ~0.8–1.2 kPa in veg and 1.0–1.5 kPa in flower. Use the VPD calculator in Tools."),
        ("Photograph weekly", "Same spot, same angle, same time of day. Your before & after comparisons will be much more useful."),
        ("Add height to photos", "Enter the height when you save a growth photo and PlantHelper builds your growth-rate chart automatically."),
        ("Light distance matters", "Leaves bleaching or curling up under LEDs? Raise the light or dim it. Stretching seedlings need more light."),
        ("Room-temperature water", "Cold water shocks roots. Aim for 18–22 °C (65–72 °F) for watering and nutrient solutions."),
        ("Airflow prevents mould", "A gentle breeze through the canopy strengthens stems and keeps powdery mildew and botrytis away."),
        ("Inspect leaf undersides", "Spider mites, aphids and whiteflies hide under leaves. A weekly check catches them before they spread."),
        ("Yellow lower leaves?", "Old leaves yellowing from the bottom up often means nitrogen deficiency, or natural fading late in flower."),
        ("Cal-Mag for coco", "Coco coir holds on to calcium and magnesium. Most coco growers add Cal-Mag to every feed."),
        ("Don't overpot seedlings", "Too much wet soil around tiny roots invites rot. Pot up gradually as roots fill the container."),
        ("Track runoff", "Runoff EC much higher than input? Salts are building up. Flush with plain pH'd water."),
        ("DLI beats wattage", "Daily light integral combines intensity and hours. Seedlings like 6–12, fruiting plants 20–40 mol/m²/day."),
        ("Prune with a purpose", "Remove leaves that shade flowers or touch the soil. Clean tools between plants to avoid spreading disease."),
        ("Harden off outdoor plants", "Move indoor-raised seedlings outside for a few hours a day over a week to avoid sun and wind shock."),
        ("Mulch saves water", "A layer of mulch keeps outdoor soil moist longer and roots cooler in summer."),
        ("Back up your journal", "Export a backup from Settings now and then, and keep it somewhere safe. Your grow history is valuable."),
        ("Label everything", "Note the variety and source when you plant. You'll thank yourself when a plant does great."),
        ("Less is more with nutrients", "Most plant problems in beginner grows come from overfeeding, not underfeeding."),
    ];

    public static (string Title, string Text) ForDay(DateTime date) => Tips[date.DayOfYear % Tips.Length];
}
