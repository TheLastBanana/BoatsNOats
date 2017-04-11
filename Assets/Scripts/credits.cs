using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class credits : MonoBehaviour {

    private float speed = 0.2f;
    public bool crawling = true;
    private RectTransform rect;
    public logoscript logoScript;
    private bool once = true;
    // Use this for initialization
    void Start () {
        // init text here, more space to work than in the Inspector (but you could do that instead)
        Text tc = GetComponent<Text>();
        string creds = "Gemma's Great Gambit\n";
        creds +="\nCredits \n";
        creds += "\nProducer:\nDeanna Dombroski\n";
        creds += "\nLead Designer:\nMarek Buchanan\n";
        creds += "\nArtist:\nDeanna Dombroski\n";
        creds += "\nWriting:\n Marek Buchanan\n";
        creds += "\nProgramming:\nStuart Bildfell\nElliot Colp\nBraedy Kuzma\nMickael Zerihoun\n";
        creds += "\nAnimators:\nDeanna Dombroski\nElliot Colp\n";
        creds += "\nSound Design:\nElliot Colp\n";
        creds += "\nMusic:\nElliot Colp\n";
        
        tc.text = creds;
        rect = transform.GetComponent<RectTransform>();
    }
	
	// Update is called once per frame
	void Update () {
        
        rect.sizeDelta = new Vector2(rect.rect.width, rect.rect.height+5f);
        if(rect.rect.height > 12384f)
        {
            if (once)
            {
                logoScript.startfade();
                once = false;
            }
            
        }
    }
}
