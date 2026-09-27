using System;
using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    public sealed class MapEdges : MonoBehaviour
    {
        [Serializable] struct Edge
        {
            public Collider2D Shape;
            public float Damage;
            public Vector2 Force;
        }

        struct Contact
        {
            public ulong Player;
            public uint Mask;
        }

        [SerializeField] BattleMap _map;
        [SerializeField] Edge[] _edges;
        [SerializeField] SoundCue _sound;
        readonly List<Contact> _contacts = new List<Contact>(2);
        const float EnterDistance = 0.035f;
        const float ExitDistance = 0.07f;

        public void Touch(NetPlayer player)
        {
            if (!_map.IsSpawned || !_map.IsServer) return;
            NetGame game = NetGame.Current;
            if (!game.CanFight)
            {
                _contacts.Clear();
                return;
            }

            int index = 0;
            while (index < game.Players.Count && game.Players[index] != player) index++;
            if (index == game.Players.Count) return;
            while (_contacts.Count <= index) _contacts.Add(default);
            Contact contact = _contacts[index];
            if (!player.CanAct || contact.Player != player.NetworkObjectId) contact.Mask = 0;
            contact.Player = player.NetworkObjectId;
            if (player.CanAct)
            {
                contact.Mask = Read(player.Collider, contact.Mask, out float damage, out Vector2 force);
                if (damage > 0f)
                    player.Health.ReceiveDamage(new DamageRequest(null, damage, DamageTag.Environment));
                if (force.sqrMagnitude > 0f)
                {
                    player.Drive.ApplyForce(force, ForceMode2D.Impulse);
                    game.Sounds.Play(_sound);
                }
            }
            _contacts[index] = contact;
        }

        uint Read(Collider2D shape, uint previous, out float damage, out Vector2 force)
        {
            uint current = 0;
            damage = 0f;
            force = Vector2.zero;
            for (int i = 0; i < _edges.Length; i++)
            {
                Edge edge = _edges[i];
                if (!edge.Shape.enabled || !edge.Shape.gameObject.activeInHierarchy) continue;
                uint bit = 1u << i;
                bool wasTouching = (previous & bit) != 0;
                ColliderDistance2D distance = shape.Distance(edge.Shape);
                if (!distance.isValid || distance.distance > (wasTouching ? ExitDistance : EnterDistance)) continue;
                current |= bit;
                if (wasTouching) continue;
                damage = Mathf.Max(damage, edge.Damage);
                force += edge.Force;
            }
            return current;
        }

        void OnDisable() => _contacts.Clear();
    }
}
