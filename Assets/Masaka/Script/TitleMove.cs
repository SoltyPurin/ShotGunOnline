using UnityEngine;

public class TitleMove : MonoBehaviour
{
    [SerializeField]
    private MoveScene _moveHonpen;
    void Start()
    {
        
    }

    void Update()
    {
        if(Input.anyKeyDown){
            _moveHonpen.LoadHonpen();
        }
    }
}
