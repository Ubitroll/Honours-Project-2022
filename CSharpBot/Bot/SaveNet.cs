using System.Numerics;
using Bot.Utilities.Processed.FieldInfo;
using Bot.Utilities.Processed.BallPrediction;
using Bot.Utilities.Processed.Packet;
using RLBotDotNet;
using System;

namespace Bot
{
    // State to Save net from ball
    public class SaveNet : BaseState
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

        // Dodge variables
        // How long to hold the jumps
        private float jumpDuration = 0.1f;
        //How long to wait after first jump
        private float waitAfterJump = 0.1f;
        // In game time at which the dodge started
        private float startTime = 0;

        // If the ball is within 30 of the AI's goal then defence is viable.
        public override bool IsViable(Bot agent, Packet packet, FieldInfo fieldInfo, BallPrediction prediction)
        {
            return Vector3.Distance(packet.Ball.Physics.Location, fieldInfo.Goals[agent.Team].Location) < 4000;            
        }

        public override Controller? GetOutput(Bot agent, Packet packet, FieldInfo fieldInfo, BallPrediction prediction)
        {
            // Variables
            Vector3 ballLocation = packet.Ball.Physics.Location;
            Vector3 agentGoalLocation = fieldInfo.Goals[agent.Index].Location;
            Vector3 agentLocation = packet.Players[agent.Index].Physics.Location;

            Vector3 ballOffset;

            // If AI is between ball and own net.
            if (Vector3.Distance(agentLocation, agentGoalLocation) < Vector3.Distance(ballLocation, agentGoalLocation))
            {
                // Find time till intercept
                float timeTillIntercept = Vector3.Distance(ballLocation, agentLocation) / 1410;

                // Truncate to whole number
                int Rounded = (int)Math.Round(timeTillIntercept);

                // Find point of interception
                Vector3 ballIntercept = prediction.Slices[0].Physics.Location;

                // Steer towards point of interception
                // Find the relative between the target and the car current location then steer towards it.
                Orientation carRotation = packet.Players[agent.Index].Physics.Rotation;
                Vector3 relativeFinalTarget = Orientation.RelativeLocation(agentLocation, ballIntercept, carRotation);

                // Steer towards final target
                if (relativeFinalTarget.Y > 0)
                {
                    steer = 1;
                }
                else
                {
                    steer = -1;
                }

                // If ball is in the air jump
                if (Vector3.Distance(ballLocation, agentLocation) < 300 && ballLocation.Z > agentLocation.Z)
                {
                    jump = true;
                }
            }
            // Else if ball is closer to goal than AI
            else if(Vector3.Distance(agentLocation, agentGoalLocation) > Vector3.Distance(ballLocation, agentGoalLocation))
            {
                // Move alonside then dodge into ball
                if(agentLocation.X > ballLocation.X)
                {
                    ballOffset = ballLocation + new Vector3(200, 0, 0);
                }
                else
                {
                    ballOffset = ballLocation - new Vector3(200, 0, 0);
                }

                // Steer towards point of interception
                // Find the relative between the target and the car current location then steer towards it.
                Orientation carRotation = packet.Players[agent.Index].Physics.Rotation;
                Vector3 relativeFinalTarget = Orientation.RelativeLocation(agentLocation, ballOffset, carRotation);

                // Steer towards final target
                if (relativeFinalTarget.Y > 0)
                {
                    steer = 1;
                }
                else
                {
                    steer = -1;
                }

                // If reach the location then Dodge
                if(Vector3.Distance(agentLocation, ballOffset) < 100)
                {
                    if (startTime == 0)
                    {
                        startTime = packet.GameInfo.SecondsElapsed;
                    }

                    // Find relative between car and ball
                    Vector3 relativeToBall = Orientation.RelativeLocation(agentLocation, ballLocation, carRotation);

                    // Use relative to decide on pitch or yaw
                    if (relativeToBall.X > 0.1)
                    {
                        yaw = 1;
                    }
                    else if (relativeToBall.X < -0.1)
                    {
                        yaw = -1;
                    }

                    if(relativeToBall.Y > 0.1)
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
                        return new Controller {Pitch = pitch, Yaw = yaw };

                    // Second jump
                    if (timeSinceStart < jumpDuration * 2 + waitAfterJump)
                        return new Controller { Jump = true, Pitch = pitch, Yaw = yaw };

                    // If the dodge is finished then reset so can dodge again
                    if (packet.Players[agent.Index].HasWheelContact)
                        startTime = 0;

                }
            }

            // If the AI hit the ball then swap state.
            if (Vector3.Distance(packet.Ball.Physics.Location, fieldInfo.Goals[agent.Index].Location) > 4000)
                return null;
            // else follow controls to get to nearest boost
            else
                return new Controller { Steer = steer, Throttle = 1};

        }
    }
}