using System;
using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    public sealed class MapRotation : MonoBehaviour
    {
        [SerializeField] BattleMap[] _maps;
        MapBag _bag;
        BattleMap _current;
        int _index;

        public IReadOnlyList<BattleMap> Prefabs => _maps;

        public void Begin()
        {
            if (_maps.Length < Rounds.SetsToWin * 2 - 1)
                throw new InvalidOperationException("대전 맵은 7개 이상 필요합니다.");
            _bag = new MapBag(_maps.Length, UnityEngine.Random.Range(0, int.MaxValue));
            Restart(true);
        }

        public void Restart(bool next)
        {
            if (next) _index = _bag.Next();
            if (_current != null)
            {
                _current.gameObject.SetActive(false);
                _current.NetworkObject.Despawn();
            }
            _current = Instantiate(_maps[_index]);
            _current.NetworkObject.Spawn(true);
        }
    }
}
