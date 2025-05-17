using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualCardsHandler : MonoBehaviour
{

    public static VisualCardsHandler instance;

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(instance.gameObject);
            instance = this;
            return;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
