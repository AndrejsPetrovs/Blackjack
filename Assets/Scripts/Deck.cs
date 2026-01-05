using UnityEngine;

public class Deck : MonoBehaviour
{
    [SerializeField] private int[] activeDeck;
    private readonly System.Random rng = new();
    private int position;

    // Changeable parameters
    public int deckCount = 1;
    public float reshufflePart = 0.25f;


    // Called when the script is loaded
    void Start()
    {
        ResetActiveDeck();
    }

    public int CardsRemaining()
    {
        return position + 1;
    }

    public void ResetActiveDeck()
    {
        activeDeck = new int[52 * deckCount];
        for (int i = 0; i < deckCount * 52; i++)
        {
            activeDeck[i] = i % 52;
        }

        // Shuffle with Fisher–Yates algorithm

        for (int n = activeDeck.Length - 1; n >= 0; n--)
        {
            int randint = rng.Next(n + 1);
            (activeDeck[randint], activeDeck[n]) = (activeDeck[n], activeDeck[randint]);
        }

        position = activeDeck.Length - 1;
    }
    
    public int DrawCard()
    {
        if (position < 0)
        {
            return -1;
        }

        int card = activeDeck[position];
        position--;
        return card;
    }
}
