using System.Numerics;
using Bot.Utilities.Processed.FieldInfo;
using Bot.Utilities.Processed.BallPrediction;
using Bot.Utilities.Processed.Packet;
using RLBotDotNet;

namespace Bot
{
    // State to take a shot at enemy goal
    public class TakeShot : BaseState
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

        public override bool IsViable(Bot agent, Packet packet, FieldInfo fieldInfo, BallPrediction prediction)
        {
            int enemyTeam;
            if (agent.Index == 0)
                enemyTeam = 1;
            else
                enemyTeam = 0;

            return Vector3.Distance(packet.Players[agent.Index].Physics.Location, fieldInfo.Goals[enemyTeam].Location) < 5000;
        }

        public override Controller? GetOutput(Bot agent, Packet packet, FieldInfo fieldInfo, BallPrediction prediction)
        {
            // Output code here

            Vector3 leftMostTarget;
            Vector3 rightMostTarget;
            float jumpDuration = 0.1f;
            float waitAfterJump = 0.1f;
            float startTime = 0.1f;


            // Retrieve relavent data to make a save
            if (agent.Team == 0)
            {
                leftMostTarget = new Vector3(800f, 5120f, 321f);
                rightMostTarget = new Vector3(-800f, 5120f, 321f);
            }
            else
            {
                leftMostTarget = new Vector3(-800f, -5120f, 321f);
                rightMostTarget = new Vector3(800f, -5120f, 321f);
            }

            Vector3 carLocation = packet.Players[agent.Index].Physics.Location;
            Vector3 ballLocation = packet.Ball.Physics.Location;

            // Get directional vectors
            Vector3 carToBall = ballLocation - carLocation;
            Vector3 carToBallDirection = Vector3.Normalize(carToBall);

            Vector3 ballToLeftTargetDirection = Vector3.Normalize(leftMostTarget - ballLocation);
            Vector3 ballToRightTargetDirection = Vector3.Normalize(rightMostTarget - ballLocation);

            // Use clamp method to get the direction of approach
            Vector3 directionOfAppraoch = Clamp2D(carToBallDirection, ballToLeftTargetDirection, ballToRightTargetDirection);

            // Offset ball location by radius of the ball in direction of where we want to approach from to find where we want to hit the ball from.
            Vector3 offsetBallLocation = ballLocation - (directionOfAppraoch * 92.75f);

            // Circle ball until direction matches close approx of direction of approach
            int sideOfApproachDirection = System.Math.Sign(Vector3.Dot(Vector3.Cross(directionOfAppraoch, new Vector3(0, 0, 1)), ballLocation - carLocation));

            Vector3 carToBallPerpendicular = Vector3.Normalize(Vector3.Cross(carToBall, new Vector3(0, 0, sideOfApproachDirection)));
            float adjustment = System.Math.Abs(AngleBetween(Flatten(carToBall), Flatten(directionOfAppraoch))) * 2560;
            Vector3 finalTarget = offsetBallLocation + (carToBallPerpendicular * adjustment);

            // Find the relative between the target and the car current location then steer towards it.
            Orientation carRotation = packet.Players[agent.Index].Physics.Rotation;
            Vector3 relativeFinalTarget = Orientation.RelativeLocation(carLocation, finalTarget, carRotation);

            // Steer towards final target
            if (relativeFinalTarget.Y > 0)
            {
                steer = 1;
            }
            else
            {
                steer = -1;
            }

            // If the car is traveling in the correct direction then turn on the boost.
            if (relativeFinalTarget.Y < 5 && relativeFinalTarget.Y > -5)
            {
                boost = true;
            }
            else boost = false;

            // If reach the location then Dodge
            if (Vector3.Distance(carLocation, finalTarget) < 100)
            {
                if (startTime == 0)
                {
                    startTime = packet.GameInfo.SecondsElapsed;
                }

                // Find relative between car and ball
                Vector3 relativeToBall = Orientation.RelativeLocation(carLocation, ballLocation, carRotation);

                // Use relative to decide on pitch or yaw
                if (relativeToBall.X > 0.1)
                {
                    yaw = 1;
                }
                else if (relativeToBall.X < -0.1)
                {
                    yaw = -1;
                }

                if (relativeToBall.Y > 0.1)
                {
                    pitch = -1;
                }
                else if (relativeToBall.Y < 0.1)
                {
                    pitch = 1;
                }

                float timeSinceStart = packet.GameInfo.SecondsElapsed - startTime;

                // First jump
                if (timeSinceStart < jumpDuration)
                    return new Controller { Jump = true };

                // Wait after the first jump
                if (timeSinceStart < jumpDuration + waitAfterJump)
                    return new Controller { Pitch = pitch, Yaw = yaw };

                // Second jump
                if (timeSinceStart < jumpDuration * 2 + waitAfterJump)
                    return new Controller { Jump = true, Pitch = pitch, Yaw = yaw };

                // If the dodge is finished then reset so can dodge again
                if (packet.Players[agent.Index].HasWheelContact)
                    startTime = 0;

            }

            


            // Method to find angle between two vectors
            float AngleBetween(Vector3 vector1, Vector3 vector2)
            {
                float dotProduct = Vector3.Dot(vector1, vector2);
                double magnitude = System.Math.Sqrt((vector1.X * vector1.X) + (vector1.Y * vector1.Y) + (vector1.Z * vector1.Z)) * System.Math.Sqrt((vector2.X * vector2.X) + (vector2.Y * vector2.Y) + (vector2.Z * vector2.Z));
                double angleRadians = System.Math.Acos(dotProduct / magnitude);

                return (float)angleRadians;
            }

            // Method to flatten vector - sets z value to 0
            Vector3 Flatten(Vector3 vector)
            {
                Vector3 flattenedVector = new Vector3(vector.X, vector.Y, 0);

                return flattenedVector;
            }

            // Method to change the carToBallDirection vector so it lies between the two target points
            Vector3 Clamp2D(Vector3 direction, Vector3 start, Vector3 end)
            {
                bool isRight = Vector3.Dot(direction, Vector3.Cross(end, new Vector3(0, 0, -1))) < 0;
                bool isLeft = Vector3.Dot(direction, Vector3.Cross(start, new Vector3(0, 0, -1))) > 0;

                if ((Vector3.Dot(end, Vector3.Cross(start, new Vector3(0, 0, -1))) > 0) ? (isRight && isLeft) : (isRight || isLeft))
                    return direction;

                if (Vector3.Dot(start, direction) < Vector3.Dot(end, direction))
                    return end;

                return start;
            }

            int enemyTeam;
            if (agent.Index == 0)
                enemyTeam = 1;
            else
                enemyTeam = 0;

            // If the AI hit the ball then swap state.
            if (Vector3.Distance(ballLocation, carLocation) <= 100)
                return null;
            else if (Vector3.Distance(packet.Players[agent.Index].Physics.Location, fieldInfo.Goals[enemyTeam].Location) > 5000)
                return null;
            // else follow controls to get to nearest boost
            else
                return new Controller { Steer = steer, Throttle = 1, Boost = boost, Jump = jump };
        }
    }
}