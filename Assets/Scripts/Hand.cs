using System.Collections.Generic;
using System.Numerics;

public class Hand
{
    public int player;
    public int state = 1;
    public float bet = 1;
    public List<int> cards = new List<int>();
    private int handSum = 0;
    public int soft = 0; // Tracks the number of Aces counted as 11
    public bool active = true;
    public int blackjack = 0;

    public Hand(int playerNum)
    {
        player = playerNum;
    }

    public static int GetCardValue(int card)
    {
        card = card % 13 + 2;
        if (card > 10 && card < 14) card = 10;
        else if (card == 14) card = 11;

        return card;
    }

    public void AddCard(int card)
    {
        cards.Add(card);
        int cardVal = GetCardValue(card);
        if (cardVal == 11) soft++;

        handSum += cardVal;
        if (soft > 0 && handSum > 21)
        {
            handSum -= 10;
            soft--;
        }
    }

    public int GetSum()
    {
        return handSum;
    }
    
    public int Splittable() // returns the value of either card if the hand can be split, 0 otherwise
    {
        if (cards.Count == 2 && GetCardValue(cards[0]) == GetCardValue(cards[1])) return GetCardValue(cards[0]);
        return 0;
    }
}
