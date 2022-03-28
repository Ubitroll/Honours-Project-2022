using System.Drawing;
using System.Numerics;
using Bot.Utilities.Processed.BallPrediction;
using Bot.Utilities.Processed.FieldInfo;
using Bot.Utilities.Processed.Packet;
using RLBotDotNet;

namespace Bot
{
    // We want to our bot to derive from Bot, and then implement its abstract methods.
    public class Bot : RLBotDotNet.Bot
    {
        private readonly StateHandler stateHandler;
        public FieldInfo myCustomFieldInfo;


        public Bot(string name, int team, int index) : base(name, team, index)
        {
            stateHandler = new StateHandler(this);

        }

        public override Controller GetOutput(rlbot.flat.GameTickPacket gameTickPacket)
        {
            Packet packet = new Packet(gameTickPacket);
            Controller output = stateHandler.GetOutput(packet);
            myCustomFieldInfo = GetFieldInfo();


            return output;
        }

        internal new FieldInfo GetFieldInfo() => new FieldInfo(base.GetFieldInfo());
        internal new BallPrediction GetBallPrediction() => new BallPrediction(base.GetBallPrediction());
    }

    // Class responsible for running and selecting the states for the bot
    public class StateHandler
    {
        private readonly Bot agent;
        private BaseState currentState;
        private (int, int) prevFrameScore = (0, 0);
        private FieldInfo fieldInfo;


        // Constructor
        public StateHandler(Bot agent)
        {
            this.agent = agent;
            fieldInfo = agent.GetFieldInfo();
        }

        // Chooses first viable state
        private BaseState SelectState(Packet packet)
        {
            // States in order of importance
            BaseState[] states =
            {
                new SaveNet(),
                new TakeShot(),
                new GetBoost(),
            };

            foreach (BaseState state in states)
                if (state.IsViable(agent, packet, fieldInfo))
                    return state;

            // if no states are viable return state that is always viable
            return new ChaseBall();
        }

        // Returns tuple of (Blue score, Orange score)
        private static (int, int) GetGoalScore(Packet packet)
        {
            return (packet.Teams[0].Score, packet.Teams[1].Score);
        }

        // Returns the output from the current state, and selects a
        // new state when the current state finishes.
        public Controller GetOutput(Packet packet)
        {
            // Reset currentState if a goal has been score.
            // Don't want to continue current state into new round
            (int, int) currentFrameScore = GetGoalScore(packet);
            if (currentFrameScore != prevFrameScore)
            {
                currentState = null;
                prevFrameScore = currentFrameScore;
            }

            if (currentState == null)
            {
                currentState = SelectState(packet);
            }

            Controller? stateOutput = currentState.GetOutput(agent, packet, fieldInfo);

            // return the controller if the sate is still running
            if (stateOutput.HasValue)
                return stateOutput.Value;

            // Reset currentState if it finished and select a new state
            currentState = null;
            return GetOutput(packet);
        }
    }

    //Base Class for states to inherit 
    public abstract class BaseState
    {
        // Determins if the state can run right now
        public abstract bool IsViable(Bot agent, Packet packet, FieldInfo fieldInfo);

        // Called each frame by StateHandler until returns null
        // Returns controller outputs tha the state should use and returns null when state finished
        public abstract Controller? GetOutput(Bot agent, Packet packet, FieldInfo fieldInfo);
    }

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


        // If the ball is within 30 of the AI's goal then defence is viable.
        public override bool IsViable(Bot agent, Packet packet, FieldInfo fieldInfo)
        {
            return Vector3.Distance(packet.Ball.Physics.Location, fieldInfo.Goals[agent.Index].Location) < 100;
        }

        public override Controller? GetOutput(Bot agent, Packet packet, FieldInfo fieldInfo)
        {
            // Output code here



            // If the AI has picked up a boost exit state
            if (Vector3.Distance(packet.Ball.Physics.Location, fieldInfo.Goals[agent.Index].Location) > 100)
                return null;
            // else follow controls to get to nearest boost
            else
                return new Controller { Throttle = 1, Steer = steer };

        }
    }

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

        public override bool IsViable(Bot agent, Packet packet, FieldInfo fieldInfo)
        {
            return packet.Players[agent.Index].Boost < 80;
        }

        public override Controller? GetOutput(Bot agent, Packet packet, FieldInfo fieldInfo)
        {
            // Output code here

            Vector3 leftMostTarget;
            Vector3 rightMostTarget;

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

            // If the AI hit the ball then swap state.
            if (Vector3.Distance(ballLocation, carLocation) <= 94)
                return null;
            // else follow controls to get to nearest boost
            else
                return new Controller { };
        }
    }

    // State to chase after ball
    public class ChaseBall : BaseState
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
        public override bool IsViable(Bot agent, Packet packet, FieldInfo fieldInfo)
        {
            return packet.Players[agent.Index].Boost < 80;
        }

        public override Controller? GetOutput(Bot agent, Packet packet, FieldInfo fieldInfo)
        {
            // Output code here


        }
    }

    // State to retreive Boost
    public class GetBoost : BaseState
    {
        // Variables        
        public bool hasPickedUp = false;
        public float steer;
        private int closestBoostPad = 0;


        public override bool IsViable(Bot agent, Packet packet, FieldInfo fieldInfo)
        {
            return packet.Players[agent.Index].Boost < 80;
        }

        public override Controller? GetOutput(Bot agent, Packet packet, FieldInfo fieldInfo)
        {
            // Output code here

            // Get datarequired to drive to boost pad.
            Vector3 carLocation = packet.Players[agent.Index].Physics.Location;
            Orientation carRotation = packet.Players[agent.Index].Physics.Rotation;

            // Get location of all boost pads
            for (int i = 0; i < fieldInfo.BoostPads.Length; i++)
            {
                // if the booost pad is active.
                if (packet.BoostPadStates[i].IsActive)
                {
                    // if current pad is closer than closest pad set to closest pad
                    if (Vector3.Distance(carLocation, fieldInfo.BoostPads[i].Location) < Vector3.Distance(carLocation, fieldInfo.BoostPads[closestBoostPad].Location))
                    {
                        closestBoostPad = i;
                    }
                }
            }

            Vector3 boostPadLocation = fieldInfo.BoostPads[closestBoostPad].Location;

            // Find where boost pad is relative to AI
            Vector3 ballRelativeLocation = Orientation.RelativeLocation(carLocation, boostPadLocation, carRotation);

            // If Boost pad is to our left we steer left otherwise steer right..
            if (ballRelativeLocation.Y > 0)
            {
                steer = -1;
            }
            else
            {
                steer = 1;
            }

            // If the AI has reached the boostpad then trigger state end condition.
            if (Vector3.Distance(carLocation, boostPadLocation) == 0)
            {
                hasPickedUp = true;
            }


            // If the AI has picked up a boost exit state
            if (hasPickedUp)
                return null;
            // else follow controls to get to nearest boost
            else
                return new Controller { Throttle = 1, Steer = steer };
        }
    }
}