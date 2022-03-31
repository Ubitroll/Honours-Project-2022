using System.Linq;
using System.Numerics;
using Bot.Utilities.Processed.FieldInfo;
using Bot.Utilities.Processed.Packet;
using RLBotDotNet;

namespace Bot
{
    // Class responsible for running and selecting the states for the bot
    public class StateHandler
    {
        private readonly Bot agent;
        private BaseState currentState;
        private (int, int) prevFrameScore = (0, 0);
        private FieldInfo fieldInfo;

        // Constructor
        public StateHandler(Bot agent, FieldInfo fieldInfo)
        {
            this.agent = agent;
            this.fieldInfo = fieldInfo;
        }

        // Chooses first viable state
        private BaseState SelectState(Packet packet)
        {
            // States in order of importance
            BaseState[] states =
            {
                new kickOff(),
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
            {
                // Render the state information to the screen. (Useful for debugging to make sure bot is in right state)
                // Allows for multiple agents to be displayed at once if multiple AI are running simultaneously.
                float renderTextPosY = 100 + 30 * agent.Index;
                string renderText = $"{GetStateName(currentState!)}";
                Vector2 upperLeft = new Vector2(10, renderTextPosY);
                System.Drawing.Color colour = agent.Team == 0 ? System.Drawing.Color.DarkBlue : System.Drawing.Color.DarkOrange;
                agent.Renderer.DrawRectangle2D(colour, upperLeft, renderText.Length * 20, 30, true);
                agent.Renderer.DrawString2D(renderText, System.Drawing.Color.White, upperLeft, 2, 2);
                return stateOutput.Value;
            }
                


            // Reset currentState if it finished and select a new state
            currentState = null;
            return GetOutput(packet);
        }

        // Method to get state name
        private string GetStateName(BaseState state)
        {
            return state.ToString().Split('.').Last();
        }

    }
}