using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class AugmentManager : MonoBehaviour
{
    [SerializeField] private AugmentPool _pool;
    [SerializeField] private GameObject _player;
    [SerializeField] private int _choiceCount = 3;

    private List<Augment> _currentChoices = new List<Augment>();

    private void Start()
    {
        RollChoice();
    }

    private void RollChoice()
    {
        _currentChoices = _pool.allAugments.OrderBy(x => UnityEngine.Random.value).Take(_choiceCount).ToList();
        
        Debug.Log("=== 증강 선택 (숫자키로 선택) ===");
        for (int i = 0; i < _currentChoices.Count; i++)
        {
            Debug.Log($"[{i + 1}] {_currentChoices[i]._augmentName} - {_currentChoices[i].description}");
        }
    }

    private void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)//test
        {
            RollChoice();
        }

        Key[] numberKeys = { Key.Digit1, Key.Digit2, Key.Digit3 };
        for(int i = 0; i < _currentChoices.Count && i < numberKeys.Length; i++)
        {
            if (Keyboard.current[numberKeys[i]].wasPressedThisFrame)
            {
                SelectAugment(i);
            }
        }
    }

    private void SelectAugment(int index)
    {
        Augment _chosen = _currentChoices[index];
        _chosen.Apply(_player);
        Debug.Log($"선택됨 : {_chosen._augmentName}");

        RollChoice();
    }
}
