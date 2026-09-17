using UnityEngine;

namespace SSW
{
    public sealed class MotionMotor
    {
        readonly MotionCast _cast;
        readonly MotionZones _zones;
        readonly PlayerController _motion;
        readonly float _gravityScale;

        public MotionMotor(CapsuleCollider2D shape, PlayerController motion, float gravity)
        {
            _cast = new MotionCast(shape, motion.GroundMask);
            _zones = new MotionZones(shape);
            _motion = motion;
            _gravityScale = Mathf.Abs(Physics2D.gravity.y) > 0.0001f
                ? gravity / Physics2D.gravity.y : shape.attachedRigidbody.gravityScale;
        }

        public void Step(ref MotionState state, MotionFrame input, float speed, float delta, bool playing)
        {
            bool jump = input.Jump > state.Jump;
            state.Jump = System.Math.Max(state.Jump, input.Jump);
            if (!playing)
            {
                state.Velocity = state.Surface = Vector2.zero;
                state.External = state.DropTime = 0f;
                state.Pad = false;
                state.Grounded = _cast.Grounded(state.Position, false);
                return;
            }

            state.DropTime = Mathf.Max(0f, state.DropTime - delta);
            bool dropping = state.DropTime > 0f;
            Vector2 velocity = state.Velocity - state.Surface;
            Vector2 surface = state.Grounded && velocity.y <= 0f && !dropping
                ? _cast.Carry(ref state.Position, state.Surface, delta, false, state.DropTop)
                : Vector2.zero;
            state.Surface = Vector2.zero;
            if (jump && _cast.Grounded(state.Position, dropping, state.DropTop))
            {
                if (input.Move.y < -0.5f && _cast.Platform(state.Position))
                {
                    state.DropTop = _cast.PlatformHeight;
                    state.DropTime = 0.5f;
                    velocity.y = -2f;
                    dropping = true;
                }
                else velocity.y = _motion.JumpSpeed;
            }

            float brake = _motion.KnockbackDecay;
            if (Mathf.Abs(input.Move.x) > 0.01f && Mathf.Sign(input.Move.x) != Mathf.Sign(state.External))
                brake += _motion.CounterBrake * Mathf.Abs(input.Move.x);
            state.External = Mathf.MoveTowards(state.External, 0f, brake * delta);
            velocity.x = input.Move.x * speed + state.External;
            velocity.y += Physics2D.gravity.y * _gravityScale * delta;

            MotionZones.Sample start = _zones.Read(state.Position);
            if (start.Acceleration > 0f)
                velocity.y = Mathf.MoveTowards(velocity.y, start.Rise, start.Acceleration * delta);
            if (start.Pad && !state.Pad)
                velocity.y = start.Launch;

            float horizontal = velocity.x;
            _cast.Move(ref state.Position, ref velocity, delta, dropping, state.DropTop);
            if (Mathf.Abs(horizontal) > 0.001f && Mathf.Abs(velocity.x) < 0.001f)
                state.External = 0f;
            MotionZones.Sample end = _zones.Read(state.Position);
            if (end.Pad && !start.Pad)
                velocity.y = end.Launch;
            state.Pad = end.Pad;
            state.Grounded = velocity.y <= 0f && _cast.Grounded(state.Position, dropping, state.DropTop);
            state.Surface = state.Grounded ? surface : Vector2.zero;
            state.Velocity = velocity + state.Surface;
        }
    }
}
