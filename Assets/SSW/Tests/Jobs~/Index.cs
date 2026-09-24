var deck = UnityEngine.Resources.Load<SSW.NetDeck>("Network/Deck");
var rows = new System.Collections.Generic.List<object>();
for (int i = 0; i < deck.Count; i++) rows.Add(new { id = i, path = UnityEditor.AssetDatabase.GetAssetPath(deck.At(i)), name = deck.At(i).name, job = (deck.At(i) is SSW.IJobRestrictedAugment item ? item.RequiredJob.ToString() : "Common") });
return rows;
