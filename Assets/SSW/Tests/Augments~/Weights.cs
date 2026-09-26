var stock = UnityEngine.Resources.Load<SSW.NetStock>("Network/Potions");
var empty = System.Array.Empty<AbstractPotion>();
var unlocked = new[]
{
    UnityEditor.AssetDatabase.LoadAssetAtPath<AbstractPotion>("Assets/RYU/3.SO/PotionSO/PoisonPotion.asset"),
    UnityEditor.AssetDatabase.LoadAssetAtPath<AbstractPotion>("Assets/RYU/3.SO/PotionSO/RegenPotion.asset")
};
int checks = 0;
void Check(bool valid, string name)
{
    if (!valid) throw new System.InvalidOperationException(name);
    checks++;
}
var reports = new System.Collections.Generic.List<object>();
foreach (var extras in new[] { empty, unlocked.Take(1).ToArray(), unlocked })
{
    var candidates = Enumerable.Range(0, stock.BaseCount).Select(i => stock.At(stock.BaseAt(i))).Concat(extras).ToArray();
    float total = candidates.Sum(p => p.weight);
    float before = 0f;
    foreach (var potion in candidates)
    {
        int id = stock.IndexOf(potion);
        Check(stock.Pick(extras, (before + potion.weight * 0.5f) / total) == id, potion.name + " middle of weighted range");
        Check(stock.Pick(extras, (before + potion.weight * 0.999f) / total) == id, potion.name + " upper weighted range");
        before += potion.weight;
    }
    Check(stock.Pick(extras, 0f) == stock.IndexOf(candidates[0]), "zero sample chooses first positive weight");
    Check(stock.Pick(extras, 1f) == stock.IndexOf(candidates.Last()), "inclusive one sample chooses last positive weight");
    reports.Add(new { unlocked = extras.Length, total, probabilities = candidates.Select(p => new { p.name, p.weight, fraction = p.weight / total }).ToArray() });
}
var original = stock.At(stock.BaseAt(0));
float saved = original.weight;
try
{
    original.weight = 0f;
    Check(stock.Pick(empty, 0f) != stock.IndexOf(original), "zero weight is excluded");
    original.weight = 100000f;
    Check(stock.Pick(empty, 0.5f) == stock.IndexOf(original), "source SO weight is consumed directly without copied values");
}
finally { original.weight = saved; }
return new { passed = checks, reports };
