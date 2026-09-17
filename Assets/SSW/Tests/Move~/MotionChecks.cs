using System;
using System.Collections.Generic;
using System.IO;
using SSW;
using UnityEditor;
using UnityEngine;

public static class MotionChecks
{
    static readonly List<GameObject> Objects = new List<GameObject>();
    static readonly List<string> Results = new List<string>();

    public static void Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first");
        try
        {
            GameObject player = Create("Motion Test Player", new Vector2(1000f, 0f));
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            CapsuleCollider2D shape = player.AddComponent<CapsuleCollider2D>();
            shape.size = new Vector2(1.0625f, 1.1875f);
            PlayerController controller = player.AddComponent<PlayerController>();
            var data = new SerializedObject(controller);
            data.FindProperty("_whatIsGround").intValue = 1 << 8;
            data.ApplyModifiedPropertiesWithoutUndo();
            Box("Ground", new Vector2(1000f, -2f), new Vector2(30f, 1f));
            Box("Wall", new Vector2(1010f, 1f), new Vector2(1f, 9f));
            Physics2D.SyncTransforms();
            MotionMotor motor = new MotionMotor(shape, controller, -29.43f);
            MotionState state = new MotionState { Position = new Vector2(1000f, 2f) };
            MotionFrame input = new MotionFrame { Tick = 1, Aim = Vector2.right };
            Step(motor, ref state, input, 100);
            Check(state.Grounded && Mathf.Abs(state.Position.y + 0.90625f) < 0.03f, $"Capsule settles on the floor: {state.Position}, {state.Velocity}, grounded {state.Grounded}");
            input.Jump = 1;
            Step(motor, ref state, input, 1);
            Check(state.Velocity.y > 10f && !state.Grounded, "First jump sequence responds immediately");
            Step(motor, ref state, input, 100);
            input.Move = Vector2.right;
            Step(motor, ref state, input, 150);
            Check(state.Position.x < 1009f && state.Position.x > 1008.8f && Mathf.Abs(state.Velocity.x) < 0.01f, "Capsule stops at the wall");
            GameObject platform = Box("Platform", new Vector2(1000f, 0f), new Vector2(6f, 0.3f));
            platform.GetComponent<BoxCollider2D>().usedByEffector = true;
            platform.AddComponent<PlatformEffector2D>().useOneWay = true;
            Physics2D.SyncTransforms();
            state = new MotionState { Position = new Vector2(1000f, -0.90625f), Grounded = true };
            input.Move = Vector2.zero;
            input.Jump = 1;
            Step(motor, ref state, input, 18);
            Check(state.Position.y > 1f, "Jump passes upward through a one-way platform");
            Step(motor, ref state, input, 60);
            Check(state.Grounded && state.Position.y > 0.7f && state.Position.y < 0.8f, "Falling lands on the one-way platform");
            input.Jump = 2;
            input.Move = Vector2.down;
            Step(motor, ref state, input, 35);
            Check(state.Position.y < -0.8f, "Down-jump drops through the platform");
            platform.SetActive(false);
            GameObject pad = Box("Pad", new Vector2(1000f, -1.1f), new Vector2(2f, 0.6f));
            pad.GetComponent<BoxCollider2D>().isTrigger = true;
            pad.AddComponent<JumpPad>();
            Physics2D.SyncTransforms();
            state = new MotionState { Position = new Vector2(1000f, -0.90625f), Grounded = true };
            input.Move = Vector2.zero;
            input.Jump = 0;
            Step(motor, ref state, input, 1);
            Check(state.Velocity.y > 10f, "Jump pad launches the predicted motor");
            pad.SetActive(false);
            GameObject wind = Box("Wind", new Vector2(1000f, 2f), new Vector2(3f, 6f));
            wind.GetComponent<BoxCollider2D>().isTrigger = true;
            SSW.WindZone zone = wind.AddComponent<SSW.WindZone>();
            zone.ResetMap();
            Physics2D.SyncTransforms();
            state = new MotionState { Position = new Vector2(1000f, 1f) };
            Step(motor, ref state, input, 20);
            Check(state.Velocity.y > 0f && state.Position.y > 1f, "Wind accelerates the predicted motor upward");
            state.External = 10f;
            Step(motor, ref state, input, 1, false);
            Check(state.Velocity == Vector2.zero && state.External == 0f, "Draft blocking clears residual motion");
            File.WriteAllLines("Logs/Move/motor-checks.txt", Results);
            Debug.Log("PASS " + Results.Count + " motor checks");
        }
        finally
        {
            foreach (GameObject obj in Objects) UnityEngine.Object.DestroyImmediate(obj);
            Objects.Clear();
            Physics2D.SyncTransforms();
        }
    }

    static void Step(MotionMotor motor, ref MotionState state, MotionFrame input, int frames, bool playing = true)
    {
        for (int i = 0; i < frames; i++) motor.Step(ref state, input, 7f, 0.02f, playing);
    }

    static GameObject Create(string name, Vector2 position)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = position;
        Objects.Add(obj);
        return obj;
    }

    static GameObject Box(string name, Vector2 position, Vector2 size)
    {
        GameObject obj = Create(name, position);
        obj.layer = 8;
        obj.AddComponent<BoxCollider2D>().size = size;
        return obj;
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Results.Add("PASS " + message);
    }
}
