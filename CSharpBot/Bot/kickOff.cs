using System.Numerics;
using Bot.Utilities.Processed.FieldInfo;
using Bot.Utilities.Processed.BallPrediction;
using Bot.Utilities.Processed.Packet;
using RLBotDotNet;

namespace Bot
{
    // State to chase after ball
    public class kickOff : BaseState
    {
        // Variables
        public float throttle;
        public float steer;
        public float pitch;
        public float yaw;
        public float roll;
        public bool jump;
        public bool boost;
        public bool handbrake;

        // Just hollow since state can only be called when no other state are viable.
        public override bool IsViable(Bot agent, Packet packet, FieldInfo fieldInfo, BallPrediction prediction)
        {
            return Vector3.Distance(packet.Ball.Physics.Location, new Vector3(0, 0, packet.Ball.Physics.Location.Z)) < 50;
        }

        public override Controller? GetOutput(Bot agent, Packet packet, FieldInfo fieldInfo, BallPrediction prediction)
        {
            // Output code here
            // Turn car towards ball
            Vector3 carLocation = packet.Players[agent.Index].Physics.Location;
            Vector3 ballLocation = packet.Ball.Physics.Location;
            Orientation carRotation = packet.Players[agent.Index].Physics.Rotation;

            Vector3 ballRelative = Orientation.RelativeLocation(carLocation, ballLocation, carRotation);

            if (ballRelative.Y > 0)
            {
                steer = 1;
            }
            else
            {
                steer = -1;
            }

            // If the ball has moved from the start position then swap state.
            if (Vector3.Distance(packet.Ball.Physics.Location, new Vector3(0, 0, packet.Ball.Physics.Location.Z)) > 50)
                return null;

            return new Controller { Throttle = 1, Steer = steer, Boost = true };
        }
    }
}