using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;

public class EndMovieManager : MonoBehaviour
{
    private PlayableDirector _movieDirector = default;

    private void Start()
    {
        _movieDirector = GameObject.Find("EndMovieManager").GetComponent<PlayableDirector>();
    }

    public void StartMovie()
    {
        PlayTimelineClientRpc();
    }

    [ClientRpc]
    public void PlayTimelineClientRpc()
    {
        if (_movieDirector != null)
        {
            _movieDirector.Play();
        }
    }

}
