using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // UI
    [SerializeField] private UIManager UIManager;
    [SerializeField] private GameObject choiceController;
    [SerializeField] private GameObject msgWindow;
    // Textures
    [SerializeField] private GameObject tableBackground;
    public Deck deck;
    [SerializeField] private GameObject[] cardPrefabs;
    [SerializeField] private GameObject cardBack;
    [SerializeField] private GameObject highlight;
    [SerializeField] private GameObject nameplate;

    // Player Hand Positions
    [SerializeField] private float leftend;
    [SerializeField] private float rightend;
    [SerializeField] private float padding;
    private float vertical;
    [SerializeField] private float nameplateOffset = 0.5f;

    // Scaling/Position Extra
    [SerializeField] private float cardScale = 0.5f;
    [SerializeField] private GameObject handsParentObj;
    private float cardWidth;
    [SerializeField] private float dealerHandVertical;

    // Technical
    private bool startedOnce = false;
    private List<int> optionsGlobal;
    public int playerCount;
    private List<Hand> hands;
    private Hand dealerHand;
    private bool dealerHandRevealed;
    private bool roundGoing = false;
    private Hand currentHand;
    private string[] messages;
    [SerializeField] private int msgWindowSize = 10;
    public int betmult = 1;

    // Stats
    private float[] winnings;
    private float[] winningsTotal;
    private int hintsUsed = 0;
    private int decisionsTotal = 0;
    private int decisionsCorrectBasic = 0;
    
    // Called when the script is loaded
    void Start()
    {
        messages = new string[msgWindowSize];

        cardWidth = cardScale * 2;
        vertical = handsParentObj.transform.position.y;
        winningsTotal = new float[4];
        for (int i = 0; i < winningsTotal.Length; i++) winningsTotal[i] = 0;
        hands = new List<Hand>(playerCount);
    }

    // Called on every frame
    async Task Update()
    {
        if (UIManager.activeMenu == 0) // If no menus are opened
        {
            if (!startedOnce)
            {
                startedOnce = true;
                GameRound();
            }
            if (startedOnce)
            {
                DisplayAllHands();
                DisplayMsgWindow();
            }

            // Handle input other than player actions
            if ((Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !roundGoing)
            {
                GameRound();
            }

            if (Input.GetKeyDown(KeyCode.S) && !roundGoing)
            {
                ShowStats();
            }

            if (Input.GetKeyDown(KeyCode.H) && currentHand.player == 0)
            {
                await GetHint();
            }
        }
    }

    #region Gameflow

    // A single full round
    private async Task GameRound()
    {
        hands.Clear();
        messages = new string[msgWindowSize];
        winnings = new float[4];
        for (int i = 0; i < winnings.Length; i++) winnings[i] = 0;
        roundGoing = true;

        dealerHand = new Hand(-1);
        dealerHandRevealed = false; // Tracks if both dealer's cards should be shown

        // Shuffle the deck if reshuffle part is reached
        if (deck.CardsRemaining() <= deck.reshufflePart * deck.deckCount * 52)
        {
            deck.ResetActiveDeck();
            AddMessage("Deck Reshuffled");
        }

        for (int i = 0; i < playerCount; i++)
        {
            hands.Add(new Hand(i));
        }

        AddMessage("Round Started\n");

        // Deal cards to everyone. Task.Delay adds time between cards being dealt
        foreach (Hand hand in hands)
        {
            await Task.Delay(200);
            DealCard(hand);
        }
        await Task.Delay(200);
        DealCard(dealerHand);
        foreach (Hand hand in hands)
        {
            await Task.Delay(200);
            DealCard(hand);
        }
        await Task.Delay(200);
        DealCard(dealerHand);
        await Task.Delay(200);

        DisplayAllHands();

        // Handle dealer having a blackjack
        if (dealerHand.GetSum() == 21)
        {
            dealerHandRevealed = true;
            foreach (Hand hand in hands)
            {
                hand.state = 0; // Means the hand is done
            }
        }

        // Fully processes all player or bot hands one by one
        for (int i = 0; i < hands.Count; i++)
        {
            while (hands[i].state != 0)
            {
                currentHand = hands[i];
                await ProcessHand(hands[i], i);
            }
        }

        // Deal to the dealer
        dealerHandRevealed = true;
        while (dealerHand.state != 0)
        {
            currentHand = dealerHand;
            await ProcessHand(dealerHand, -1);
        }

        // Notify of round end condition
        if (dealerHand.GetSum() == 21 && dealerHand.cards.Count == 2)
        {
            AddMessage("\nRound over: Dealer has Blackjack");
        }
        else
        {
            AddMessage("\nRound over: Dealer has " + dealerHand.GetSum().ToString());
        }

        // Calculate results, adjust winnings
        foreach (Hand hand in hands)
        {
            if (!hand.active) continue;

            float before = winnings[hand.player];

            if (hand.GetSum() > 21)
            {
                winnings[hand.player] -= hand.bet * betmult;
            }
            else if (dealerHand.GetSum() > 21)
            {
                winnings[hand.player] += hand.bet * betmult;
            }
            else if (hand.GetSum() > dealerHand.GetSum())
            {
                winnings[hand.player] += hand.bet * betmult;
            }
            else if (hand.GetSum() < dealerHand.GetSum())
            {
                winnings[hand.player] -= hand.bet * betmult;
            }
        }

        // Notify about results
        for (int i = 0; i < playerCount; i++)
        {
            string msg = "";
            if (i == 0) msg += "You";
            else msg += "Bot " + i.ToString();
            if (winnings[i] > 0) msg += " won " + winnings[i].ToString() + " x Original Bet";
            if (winnings[i] < 0) msg += " lost " + (-winnings[i]).ToString() + " x Original Bet";
            if (winnings[i] == 0) msg += " did not win or lose anything";
            AddMessage(msg);

            winningsTotal[i] += winnings[i];
        }

        AddMessage("\nYour total balance change: " + winningsTotal[0].ToString() + " x Original Bet");

        // End of Round
        currentHand = null;
        roundGoing = false;

        AddMessage("\nPress [Space] to start a new round, [S] to see statistics");
    }

    private async Task GetHint()
    {
        int suggested = await GetDecision(currentHand, optionsGlobal, true); // Get correct basic strategy-based decision
        string hintmsg = "(Hint) Correct basic strategy choice: ";
        if (suggested == 0) hintmsg += "Stand";
        else if (suggested == 1) hintmsg += "Hit";
        else if (suggested == 2) hintmsg += "Double Down";
        else if (suggested == 3) hintmsg += "Split";
        else hintmsg = "Error: Unexpected hint suggestion";

        hintsUsed++;

        AddMessage(hintmsg);
    }

    private void ShowStats()
    {
        // Shows number of correct decisions, total decisions, correct decision percentage, and number of hints used
        string statsmsg = "\nCorrect decisions based on basic strategy: \n" + decisionsCorrectBasic.ToString() + " out of " + decisionsTotal.ToString() + " ( " + (100f * decisionsCorrectBasic / MathF.Max(decisionsTotal, 1)).ToString() + " % )\n";
        AddMessage(statsmsg);
        statsmsg = "Hints used: " + hintsUsed.ToString();
        AddMessage(statsmsg);
    }

    private void DisplayMsgWindow() // Updates message window to include new messages, called in Update() on very frame
    {
        string fulltxt = "";
        for (int i = msgWindowSize - 1; i >= 0; i--)
        {
            if (messages[i] != null)
                fulltxt += messages[i] + "\n";
        }
        msgWindow.GetComponent<TextMeshProUGUI>().text = fulltxt;
    }
    
    private void AddMessage(string msg)
    {
        for (int i = msgWindowSize - 1; i >= 1; i--)
        {
            messages[i] = messages[i - 1];
        }
        messages[0] = msg;
    }

    private float GetHandWidth() // Evaluates maximum had width for display based on number of hands
    {
        int activeHands = 0;
        foreach (Hand hand in hands)
        {
            if (hand.active) activeHands++;
        }

        return (rightend - leftend - padding * (activeHands - 1)) / activeHands;
    }

    private void HighlightHand(float posX, float posY, float width, float height)
    {
        GameObject newHighlight = Instantiate(highlight, new Vector2(posX, posY), Quaternion.identity, handsParentObj.transform);
        newHighlight.transform.localScale = new Vector2(width, height);
    }
    
    private void DisplayHand(Hand hand, float posX, float posY, float width)
    {
        // Back-to-back horizontally
        float cardPosX = posX - (hand.cards.Count - 1) * cardWidth / 2;
        float cardPosXinitial = cardPosX;

        foreach (int card in hand.cards)
        {
            GameObject newCard = Instantiate(cardPrefabs[card], new Vector2(cardPosX, posY), Quaternion.identity, handsParentObj.transform);
            newCard.transform.localScale = new Vector2(1, 1) * cardScale;
            cardPosX += cardWidth;
        }

        // Nameplate
        string name = "";
        if (hand.player == -1) name = "Dealer";
        else if (hand.player == 0) name = "You";
        else name = "Bot " + hand.player.ToString();
        GameObject newNameplate = Instantiate(nameplate, new Vector2(posX, posY - nameplateOffset), Quaternion.identity, handsParentObj.transform);
        newNameplate.GetComponent<TextMeshPro>().text = name;

        // Hide Dealer Card
        if (hand.player == -1 && !dealerHandRevealed && hand.cards.Count > 0)
        {
            GameObject newCard = Instantiate(cardBack, new Vector2(cardPosXinitial, posY), Quaternion.identity, handsParentObj.transform);
            newCard.transform.localScale = new Vector2(1, 1) * cardScale;
        }
        
        if (hand == currentHand)
        {
            float padding = 0.2f;
            float hwidth = hand.cards.Count * 0.6f + padding * 2;
            float hheight = 0.75f + padding * 2;
            HighlightHand(posX, posY, hwidth, hheight);
        }
    }

    private void DisplayAllHands()
    {
        while (handsParentObj.transform.childCount > 0) {
            DestroyImmediate(handsParentObj.transform.GetChild(0).gameObject);
        }
        DisplayPlayerHands();
        DisplayHand(dealerHand, 0, dealerHandVertical, GetHandWidth());
    }
    private void DisplayPlayerHands() // Displays all player and bot hands, but not dealer hand
    {
        float handWidth = GetHandWidth();
        float posX = rightend - handWidth / 2;

        foreach (Hand hand in hands)
        {
            if (hand.active)
            {
                DisplayHand(hand, posX, vertical, handWidth);
                posX -= handWidth + padding;
            }
        }
    }

    private void DealCard(Hand hand)
    {
        hand.AddCard(deck.DrawCard());
    }

    private async Task<int> WaitForPlayerNum() // Waits for player to enter a number tied to an action (1-4)
    {
        int chosen = 0;

        while (chosen == 0)
        {
            if (UIManager.activeMenu == 0)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) chosen = 1;
                if (Input.GetKeyDown(KeyCode.Alpha2)) chosen = 2;
                if (Input.GetKeyDown(KeyCode.Alpha3)) chosen = 3;
                if (Input.GetKeyDown(KeyCode.Alpha4)) chosen = 4;                
            }

            await Task.Yield(); // Pause for one frame
        }

        return chosen;
    }

    private async Task<int> GetDecision(Hand hand, List<int> options, bool basicStrat = false)
    {
        /* Options 
        0 - Stand
        1 - Hit
        2 - Double Down
        3 - Split
        */
        
        string msg = "";
        if (hands.Count >= 12) options.Remove(3); // Removes split option when too many hands are present - very unlikely to occur

        if (hand.player == 0 && !basicStrat)
        {
            // Await player decision
            choiceController.SetActive(true);
            choiceController.GetComponent<ChoiceController>().UpdateChoices(options);

            // Wait for a valid player choice
            int chosen = -1;
            while (options.IndexOf(chosen) < 0)
                chosen = await WaitForPlayerNum() - 1;
            choiceController.SetActive(false);

            // Adjust statistics data
            decisionsTotal++;
            int correctDecision = await GetDecision(hand, options, true);
            if (chosen == correctDecision) decisionsCorrectBasic++;

            // Notify of player decision
            if (chosen == 0) msg = "You Stand";
            else if (chosen == 1) msg = "You Hit";
            else if (chosen == 2) msg = "You Double Down";
            else if (chosen == 3) msg = "You Split";
            AddMessage(msg);
            return chosen;
        }
        else if (hand.player == -1)
        {
            // Await dealer decision
            // Based on standard rules - dealer draws until 17
            await Task.Delay(500);
            if (hand.GetSum() < 17)
            {
                AddMessage("Dealer Hits");
                return 1;
            }
            AddMessage("Dealer Stands");
            return 0;
        }
        else
        {
            // Await bot decision or correct decision for hints/statistics - always basic strategy
            if (!basicStrat) await Task.Delay(500);

            int sum = hand.GetSum();
            bool soft = hand.soft > 0;
            int dealerCard = Hand.GetCardValue(dealerHand.cards[1]);
            int chosen = 0;

            // Basic Strategy - mathematically pre-calculated optimal choices, without accounting for which cards have already left the deck
            if (options.IndexOf(3) >= 0 && sum != 10 && sum != 20)
            // If split is available. 10 and 20 are ignored since then splitting is always suboptimal
            {
                switch (sum)
                {
                    case 4:
                        if (dealerCard <= 7) chosen = 3;
                        else chosen = 1;
                        break;
                    case 6:
                        if (dealerCard <= 7) chosen = 3;
                        else chosen = 1;
                        break;
                    case 8:
                        if (dealerCard == 5 || dealerCard == 6) chosen = 3;
                        else chosen = 1;
                        break;
                    case 12:
                        if (soft || dealerCard <= 6) chosen = 3;
                        else chosen = 1;
                        break;
                    case 14:
                        if (dealerCard <= 7) chosen = 3;
                        else chosen = 1;
                        break;
                    case 16:
                        chosen = 3;
                        break;
                    case 18:
                        if (dealerCard == 7 || dealerCard >= 10) chosen = 1;
                        else chosen = 3;
                        break;
                    default:
                        Debug.Log("Split available on unexpected sum");
                        break;
                }
            }
            else if (soft) // If an Ace is still counted as 11
            {
                switch (sum)
                {
                    case 13:
                    case 14:
                        if (dealerCard == 5 || dealerCard == 6) chosen = 2;
                        else chosen = 1;
                        break;
                    case 15:
                    case 16:
                        if (dealerCard >= 4 && dealerCard <= 6) chosen = 2;
                        else chosen = 1;
                        break;
                    case 17:
                        if (dealerCard >= 3 && dealerCard <= 6) chosen = 2;
                        else chosen = 1;
                        break;
                    case 18:
                        if (dealerCard >= 3 && dealerCard <= 6) chosen = 2;
                        else if (dealerCard <= 8) chosen = 0;
                        else chosen = 1;
                        if (chosen == 2 && options.IndexOf(2) < 0) chosen = 0;
                        break;
                    case 19:
                    case 20:
                        chosen = 0;
                        break;
                    default:
                        Debug.Log("Unexpected sum");
                        break;
                }
            }
            else
            // Normal case - no splitting, no Aces as 11
            {
                switch (sum)
                {
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                        chosen = 1;
                        break;
                    case 9:
                        if (dealerCard >= 3 && dealerCard <= 6) chosen = 2;
                        else chosen = 1;
                        break;
                    case 10:
                        if (dealerCard <= 9) chosen = 2;
                        else chosen = 1;
                        break;
                    case 11:
                        if (dealerCard <= 10) chosen = 2;
                        else chosen = 1;
                        break;
                    case 12:
                        if (dealerCard >= 4 && dealerCard <= 6) chosen = 0;
                        else chosen = 1;
                        break;
                    case 13:
                    case 14:
                    case 15:
                    case 16:
                        if (dealerCard <= 6) chosen = 0;
                        else chosen = 1;
                        break;
                    case 17:
                    case 18:
                    case 19:
                    case 20:
                        chosen = 0;
                        break;
                    default:
                        Debug.Log("Unexpected sum");
                        break;
                }
            }
            // Deal with all cases when Doubling Down is impossible
            if (chosen == 2 && options.IndexOf(2) < 0) chosen = 1;

            if (options.IndexOf(chosen) < 0) Debug.Log("Forbidden option: " + chosen.ToString());

            if (!basicStrat)
            {
                // Notify of bot decision
                msg = "Bot " + currentHand.player.ToString();
                if (chosen == 0) msg += " Stands";
                else if (chosen == 1) msg += " Hits";
                else if (chosen == 2) msg += " Doubles Down";
                else if (chosen == 3) msg += " Splits";
                AddMessage(msg);
            }
            return chosen;
        }
    }
    
    private async Task ProcessHand(Hand hand, int index)
    {
        // Uses hand.state to make a single step in processing a hand
        if (hand.player != 0) await Task.Delay(100);
        if (hand.state == 1) // Normal starting state
        {
            if (hand.GetSum() == 21)
            {
                // Blackjack
                int playerHands = 0;
                foreach (Hand h in hands)
                {
                    if (h.active && h.player == hand.player) playerHands++;
                }
                if (playerHands < 2)
                {
                    hand.bet *= 1.5f;
                    hand.blackjack = 1;
                }
                hand.state = 0;
            }
            else
            {
                optionsGlobal = new List<int> { 0, 1, 2 };
                if (hand.Splittable() != 0)
                {
                    optionsGlobal.Add(3);
                }

                int decision = await GetDecision(hand, optionsGlobal);

                // Change hand state according to player decision
                if (decision == 0)
                {
                    hand.state = 0;
                }
                else if (decision == 1)
                {
                    DealCard(hand);
                    hand.state = 2;
                }
                else if (decision == 2)
                {
                    hand.bet *= 2;
                    DealCard(hand);
                    hand.state = 0;
                }
                else if (decision == 3)
                {
                    // Splitting: create two new hands, deactivate the original
                    hand.active = false;
                    Hand subhand1 = new Hand(hand.player);
                    Hand subhand2 = new Hand(hand.player);
                    subhand1.AddCard(hand.cards[0]);
                    subhand2.AddCard(hand.cards[1]);
                    if (Hand.GetCardValue(hand.cards[0]) == 11)
                    {
                        // Splitting Aces
                        subhand1.state = 4;
                        subhand2.state = 4;

                    }
                    else
                    {
                        // Splitting Non-Aces
                        subhand1.state = 3;
                        subhand2.state = 3;
                    }
                    hands.Insert(index + 1, subhand2);
                    hands.Insert(index + 2, subhand1);

                    hand.state = 0;
                }
                else
                {
                    Debug.LogError("Unexpected Decision");
                }
            }
        }
        else if (hand.state == 2) // After hitting
        {
            if (hand.GetSum() >= 21)
            {
                hand.state = 0;
            }
            else
            {
                optionsGlobal = new List<int> { 0, 1 };

                int decision = await GetDecision(hand, optionsGlobal);

                if (decision == 0)
                {
                    hand.state = 0;
                }
                else if (decision == 1)
                {
                    DealCard(hand);
                    hand.state = 2;
                }
                else
                {
                    Debug.LogError("Unexpected Decision");
                }
            }
        }
        else if (hand.state == 3) // After splitting non-aces
        // Similar to state 1, but no blackjack
        {
            DealCard(hand);
            if (hand.GetSum() >= 21)
            {
                hand.state = 0;
            }
            else
            {
                optionsGlobal = new List<int> { 0, 1, 2 };
                if (hand.Splittable() != 0)
                {
                    optionsGlobal.Add(3);
                }

                int decision = await GetDecision(hand, optionsGlobal);

                if (decision == 0)
                {
                    hand.state = 0;
                }
                else if (decision == 1)
                {
                    DealCard(hand);
                    hand.state = 2;
                }
                else if (decision == 2)
                {
                    hand.bet *= 2;
                    DealCard(hand);
                    hand.state = 0;
                }
                else if (decision == 3)
                {
                    // Split the hand into two, deactivate the original
                    hand.active = false;
                    Hand subhand1 = new Hand(hand.player);
                    Hand subhand2 = new Hand(hand.player);
                    subhand1.AddCard(hand.cards[0]);
                    subhand2.AddCard(hand.cards[1]);

                    subhand1.state = 3;
                    subhand2.state = 3;
                    
                    hands.Insert(index + 1, subhand2);
                    hands.Insert(index + 2, subhand1);

                    hand.state = 0;
                }
                else
                {
                    Debug.LogError("Unexpected Decision");
                }
            }
        }
        else if (hand.state == 4) // AFter splitting aces
        {
            DealCard(hand);
            hand.state = 0;
        }
        else
        {
            Debug.LogError("Unexpected Hand State");
        }
    }

    #endregion Gameflow
}
