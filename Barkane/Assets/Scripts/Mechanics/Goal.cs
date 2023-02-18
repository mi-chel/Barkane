using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour, IThemedItem
{

    public static event System.EventHandler OnReachGoal;

    public int numShards;
    private int numShardsCollected;

    private bool goalActive = false;
    public bool inGlowstickRange = true; //C: True except when in caves and no glowstick in area
    public bool particlesActive = true; //C: True except when in caves and no glowstick active

    [SerializeField] private GameObject inactiveGoal;
    [SerializeField] private GameObject activeGoal;
    [SerializeField] private GameObject goalPlane;

    [SerializeField] private List<Material> swirlMaterials;

    [SerializeField] private new ParticleSystem particleSystem;
    public List<Gradient> themePartColors = new List<Gradient>();

    private void OnEnable() {
        GlowStickLogic.OnGlowstickChange += OnGlowstickChange;
    }

    private void OnDisable() {
        GlowStickLogic.OnGlowstickChange -= OnGlowstickChange;
    }

    private void Start() {
        ActivateGoal(CheckIfGoalActive());
        ActivateParticles(particlesActive);
    }

    private void OnTriggerEnter(Collider other) {
        if(other.gameObject.CompareTag("Player") && goalActive)
            StartCoroutine(WaitToEndLevel());
    }

    //C: Used so player finishes moving
    private IEnumerator WaitToEndLevel() {
        yield return new WaitUntil(() => !ActionLockManager.Instance.IsLocked);
        EndLevel();
    }
    
    public void EndLevel() {
        OnReachGoal?.Invoke(this, new System.EventArgs());
       // LevelManager.Instance.EndLevel();
        UIManager.Instance.EndLevel();
    }

    public void UpdateTheme(Theme t) {
        activeGoal.GetComponent<MeshRenderer>().material = t.crystalMat;
        goalPlane.GetComponent<MeshRenderer>().material = swirlMaterials[(int)t.themeEnum];
    }

    public void CollectShard()
    {
        numShardsCollected++;
        //update shard display
        UIManager.UpdateShardCount(numShardsCollected, numShards);
        if(CheckIfGoalActive())
            ActivateGoal();
    }

    private void ActivateGoal(bool val = true)
    {
        goalActive = val;
        inactiveGoal.SetActive(!val);
        activeGoal.SetActive(val);
        goalPlane.SetActive(val);
    }

    public void SetInGlowstickRange (bool val = true)
    {
        inGlowstickRange = val;
        ActivateGoal(CheckIfGoalActive());
    }

    private bool CheckIfGoalActive()
    {
        return (numShardsCollected >= numShards && inGlowstickRange);
    }

    public void ActivateParticles(bool val)
    {
        particlesActive = val;
        if(particlesActive){
            particleSystem.Play();
        }
        else {
            particleSystem.Stop();
        }
    }

    private void OnGlowstickChange(object sender, GlowStickLogic.GlowStickArgs e) {
        if(e.state == GlowstickState.OFF)
            ActivateParticles(false);
        if(e.state == GlowstickState.CRACKED)
            ActivateParticles(true);
    }
}
