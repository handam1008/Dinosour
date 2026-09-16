using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(menuName = "Maps/Catalog")]
    public sealed class MapCatalog : ScriptableObject
    {
        [SerializeField] MapLayout[] _maps;

        public IReadOnlyList<MapLayout> Maps => _maps;
    }
}
