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

        public bool TryPlace(Vector2 target, Vector2 normal, float radius, out Vector2 position)
            => _cast.TryPlace(target, normal, radius, out position);

        public void Ride(MotionState previous, MotionState current, float delta)
        {
            if (!current.Grounded || current.DropTime > 0f) return;
            Vector2 velocity = new Vector2(current.Velocity.x, previous.Velocity.y);
            _cast.Ride(current.Position, velocity, Physics2D.gravity.y * _gravityScale, delta, !previous.Grounded);
        }

        public void Step(ref MotionState state, MotionFrame input, float speed, float delta, bool playing)
        {
            if (input.Aim.sqrMagnitude > 0.001f) state.Aim = input.Aim.normalized;
            float scale = (state.BodyScale > 0f ? state.BodyScale : 1f) * (playing && state.SmallTime > 0f ? 0.5f : 1f);
            _cast.Scale(ref state, scale);
            state.SmallTime = Mathf.Max(0f, state.SmallTime - delta);
            if (playing) _cast.Recover(ref state.Position, ref state.Velocity, state.DropTime > 0f, state.DropTop, state.Surface);
            bool jump = input.Jump > state.Jump;
            state.Jump = System.Math.Max(state.Jump, input.Jump);
            state.CoyoteTime = Mathf.Max(0f, state.CoyoteTime - delta);
            bool frozen = state.FreezeTime > 0f;
            state.FreezeTime = Mathf.Max(0f, state.FreezeTime - delta);
            if (!playing || frozen)
            {
                state.Velocity = state.Surface = Vector2.zero;
                state.External = state.DropTime = state.CoyoteTime = 0f;
                state.Pad = false;
                state.DashTime = state.BurstTime = 0f;
                state.Grounded = _cast.Grounded(state.Position, false);
                return;
            }

            if (state.BurstTime > 0f)
            {
                float travel = Mathf.Min(delta, state.BurstTime);
                state.BurstTime = Mathf.Max(0f, state.BurstTime - delta);
                Vector2 burst = state.BurstVelocity;
                _cast.Move(ref state.Position, ref burst, travel, false, 0f, state.Grounded && burst.y <= 0f);
                state.Velocity = state.BurstTime > 0f ? burst : Vector2.zero;
                state.External = 0f;
                state.Surface = Vector2.zero;
                state.Grounded = _cast.Grounded(state.Position, false);
                state.CoyoteTime = 0f;
                return;
            }
            if (state.DashTime > 0f)
            {
                float travelTime = Mathf.Min(delta, state.DashTime);
                state.DashTime = Mathf.Max(0f, state.DashTime - delta);
                if (state.DashTime < 0.000001f) state.DashTime = 0f;
                Vector2 dash = new Vector2(state.DashSpeed, 0f);
                _cast.Move(ref state.Position, ref dash, travelTime, false, 0f, state.Grounded);
                state.Velocity = dash;
                state.External = 0f;
                state.Surface = Vector2.zero;
                state.Grounded = _cast.Grounded(state.Position, false);
                if (state.Grounded) state.CoyoteTime = _motion.CoyoteTime;
                if (Mathf.Abs(dash.x) < 0.001f) state.DashTime = 0f;
                return;
            }
            state.DropTime = Mathf.Max(0f, state.DropTime - delta);
            bool dropping = state.DropTime > 0f;
            Vector2 velocity = state.Velocity - state.Surface;
            Vector2 surface = state.Grounded && velocity.y <= 0f && !dropping
                ? _cast.Carry(ref state.Position, state.Surface, delta, false, state.DropTop)
                : Vector2.zero;
            state.Surface = Vector2.zero;
            bool grounded = velocity.y <= 0f && _cast.Grounded(state.Position, dropping, state.DropTop);
            if (grounded)
            {
                state.CoyoteTime = _motion.CoyoteTime;
                state.AirUsed = 0;
            }
            else if (velocity.y > 0f) state.CoyoteTime = 0f;
            if (jump && (grounded || state.CoyoteTime > 0.0001f))
            {
                state.CoyoteTime = 0f;
                if (grounded && input.Move.y < -0.5f && _cast.Platform(state.Position))
                {
                    state.DropTop = _cast.PlatformHeight;
                    state.DropTime = 0.5f;
                    velocity.y = -2f;
                    dropping = true;
                }
                else velocity.y = _motion.JumpSpeed;
            }
            else if (jump && state.AirUsed < state.AirJumps && !dropping)
            {
                state.AirUsed++;
                velocity.y = _motion.JumpSpeed * state.AirJumpRatio;
                state.CoyoteTime = 0f;
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
            _cast.Move(ref state.Position, ref velocity, delta, dropping, state.DropTop, grounded && velocity.y <= 0f && !dropping);
            if (Mathf.Abs(horizontal) > 0.001f && Mathf.Abs(velocity.x) < 0.001f)
                state.External = 0f;
            MotionZones.Sample end = _zones.Read(state.Position);
            if (end.Pad && !start.Pad)
                velocity.y = end.Launch;
            state.Pad = end.Pad;
            state.Grounded = velocity.y <= 0f && _cast.Grounded(state.Position, dropping, state.DropTop);
            if (state.Grounded) state.CoyoteTime = _motion.CoyoteTime;
            else if (velocity.y > 0f || dropping) state.CoyoteTime = 0f;
            state.Surface = state.Grounded ? surface : Vector2.zero;
            state.Velocity = velocity + state.Surface;
        }
    }
}
