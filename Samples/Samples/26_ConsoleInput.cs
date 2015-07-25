﻿using Urho;

class _26_ConsoleInput : Sample
{
    private Scene scene;
    private bool drawDebug;
    private Camera camera;

    string[] hungerLevels = {
        "bursting",
        "well-fed",
        "fed",
        "hungry",
        "very hungry",
        "starving"
    };

    // Urho threat level descriptions
    string[] urhoThreatLevels = {
        "Suddenly Urho appears from a dark corner of the fish tank",
        "Urho seems to have his eyes set on you",
        "Urho is homing in on you mercilessly"
    };

    public _26_ConsoleInput(Context ctx) : base(ctx) { }

    public override void Start()
    {
        base.Start();
    }

    void HandleConsoleCommand(StringHash eventType)
    {
        if (eventData[P_ID].GetString() == GetTypeName())
            HandleInput(eventData[P_COMMAND].GetString());
    }

    void HandleUpdate(StringHash eventType, VariantMap& eventData)
    {
        // Check if there is input from stdin
        string input = GetConsoleInput();
        if (input.Length())
            HandleInput(input);
    }

    void HandleEscKeyDown(StringHash eventType, VariantMap& eventData)
    {
        // Unlike the other samples, exiting the engine when ESC is pressed instead of just closing the console
        if (eventData[KeyDown::P_KEY].GetInt() == KEY_ESC)
            engine_.Exit();
    }

    void StartGame()
    {
        Print("Welcome to the Urho adventure game! You are the newest fish in the tank; your\n"
              "objective is to survive as long as possible. Beware of hunger and the merciless\n"
              "predator cichlid Urho, who appears from time to time. Evading Urho is easier\n"
              "with an empty stomach. Type 'help' for available commands.");

        gameOn_ = true;
        foodAvailable_ = false;
        eatenLastTurn_ = false;
        numTurns_ = 0;
        hunger_ = 2;
        urhoThreat_ = 0;
    }

    void EndGame(string message)
    {
        Print(message);
        Print("Game over! You survived " + String(numTurns_) + " turns.\n"
              "Do you want to play again (Y/N)?");

        gameOn_ = false;
    }

    void Advance()
    {
        if (urhoThreat_ > 0)
        {
            ++urhoThreat_;
            if (urhoThreat_ > 3)
            {
                EndGame("Urho has eaten you!");
                return;
            }
        }
        else if (urhoThreat_ < 0)
            ++urhoThreat_;
        if (urhoThreat_ == 0 && NextRandom() < 0.2f)
            ++urhoThreat_;

        if (urhoThreat_ > 0)
            Print(urhoThreatLevels[urhoThreat_ - 1] + ".");

        if ((numTurns_ & 3) == 0 && !eatenLastTurn_)
        {
            ++hunger_;
            if (hunger_ > 5)
            {
                EndGame("You have died from starvation!");
                return;
            }
            else
                Print("You are " + hungerLevels[hunger_] + ".");
        }

        eatenLastTurn_ = false;

        if (foodAvailable_)
        {
            Print("The floating pieces of fish food are quickly eaten by other fish in the tank.");
            foodAvailable_ = false;
        }
        else if (NextRandom() < 0.15f)
        {
            Print("The overhead dispenser drops pieces of delicious fish food to the water!");
            foodAvailable_ = true;
        }

        ++numTurns_;
    }

    void HandleInput(string input)
    {
        string inputLower = input.ToLower().Trimmed();
        if (inputLower.Empty())
        {
            Print("Empty input given!");
            return;
        }

        if (inputLower == "quit" || inputLower == "exit")
            engine_.Exit();
        else if (gameOn_)
        {
            // Game is on
            if (inputLower == "help")
                Print("The following commands are available: 'eat', 'hide', 'wait', 'score', 'quit'.");
            else if (inputLower == "score")
                Print("You have survived " + String(numTurns_) + " turns.");
            else if (inputLower == "eat")
            {
                if (foodAvailable_)
                {
                    Print("You eat several pieces of fish food.");
                    foodAvailable_ = false;
                    eatenLastTurn_ = true;
                    hunger_ -= (hunger_ > 3) ? 2 : 1;
                    if (hunger_ < 0)
                    {
                        EndGame("You have killed yourself by over-eating!");
                        return;
                    }
                    else
                        Print("You are now " + hungerLevels[hunger_] + ".");
                }
                else
                    Print("There is no food available.");

                Advance();
            }
            else if (inputLower == "wait")
            {
                Print("Time passes...");
                Advance();
            }
            else if (inputLower == "hide")
            {
                if (urhoThreat_ > 0)
                {
                    bool evadeSuccess = hunger_ > 2 || NextRandom() < 0.5f;
                    if (evadeSuccess)
                    {
                        Print("You hide behind the thick bottom vegetation, until Urho grows bored.");
                        urhoThreat_ = -2;
                    }
                    else
                        Print("Your movements are too slow; you are unable to hide from Urho.");
                }
                else
                    Print("There is nothing to hide from.");

                Advance();
            }
            else
                Print("Cannot understand the input '" + input + "'.");
        }
        else
        {
            // Game is over, wait for (y)es or (n)o reply
            if (inputLower[0] == 'y')
                StartGame();
            else if (inputLower[0] == 'n')
                Engine.Exit();
            else
                Print("Please answer 'y' or 'n'.");
        }
    }

    void Print(string output)
    {
        // Logging appears both in the engine console and stdout
        LOGRAW(output + "\n");
    }

}