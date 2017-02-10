using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityStandardAssets._2D
{
    public class SceneChanger : MonoBehaviour
    {
        private int currentScene;
        private int nextScene;

        // Use this for initialization
        void Start()
        {
            // Make sure scenes are ordered properly in build settings
            currentScene = SceneManager.GetActiveScene().buildIndex;
            nextScene = currentScene + 1;
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(currentScene);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.tag == "Player")
            {
                SceneManager.LoadScene(nextScene);
            }
        }
    }
}
