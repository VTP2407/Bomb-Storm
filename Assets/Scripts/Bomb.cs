using System.Collections;
using UnityEngine;
using UnityEngine.Windows;

public class Bomb : MonoBehaviour
{
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void SetUp(float lifeTime)
    {
        if(anim!=null && lifeTime > 0)
        {
            anim.SetFloat("speed", 1f / lifeTime);
        }
    }
}
