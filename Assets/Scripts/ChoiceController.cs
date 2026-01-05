using System.Collections.Generic;
using UnityEngine;

public class ChoiceController : MonoBehaviour
{
    [SerializeField] private GameObject choiceStand;
    [SerializeField] private GameObject choiceHit;
    [SerializeField] private GameObject choiceDouble;
    [SerializeField] private GameObject choiceSplit;
    
    // Tracks which actions are available to the player and displays them on screen
    public void UpdateChoices(List<int> options)
    {
        if (options.IndexOf(2) > -1)
        {
            choiceDouble.SetActive(true);
        }
        else
        {
            choiceDouble.SetActive(false);
        }
        if (options.IndexOf(3) > -1)
        {
            choiceSplit.SetActive(true);
        }
        else
        {
            choiceSplit.SetActive(false);
        }
    }
}
