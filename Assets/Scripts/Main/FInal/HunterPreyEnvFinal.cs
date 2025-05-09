using UnityEngine;
using System.Collections.Generic;
using Unity.MLAgents;
using System.Collections;
using static Unity.Burst.Intrinsics.X86.Avx;

/// <summary>
/// Gère l'environnement de jeu pour un scénario de chasse entre chasseurs et proies.
/// Contrôle l'apparition, la réinitialisation et le déroulement des épisodes.
/// </summary>
public class HunterPreyEnvFinal : MonoBehaviour
{
    #region Variables

    [Header("Prefabs & Spawn")]
    public GameObject hunterPrefab;
    public GameObject preyPrefab;
    public GameObject diePrefabHunter;
    public GameObject diePrefabPrey;
    public GameObject energyPrey;
    public GameObject speedBoostPrefab;
    public GameObject camouflagePrefab;

    [Header("Game Settings")]
    public int hunterCount = 2;
    public int preyCount = 3;
    public float timeLimit = 30f;
    public int energyPreyCount = 3;

    public int speedBoostCount = 2;
    public int camouflageCount = 2;

    public float delayEnergyRespawn = 5;
    public float delaySpeedBonusRespawn = 7;
    public float delayCamouflageRespawn = 8;

    private List<GameObject> speedBoostList;
    private List<GameObject> camouflageList;

    public RoomGenerator2D roomGenerator;

    public InGameUI gameUI;

    public CommunicationBuffer commBuffer;

    private float hidingTime = 5f;
    private bool huntersReleased = false;

    private List<HunterAgentFinal> hunters;
    private List<PreyAgentFinal> preys;
    private List<HunterAgentFinal> disabledHunters;
    private List<PreyAgentFinal> disabledPreys;

    private List<GameObject> energyList;

    private float timer;

    private SimpleMultiAgentGroup hunterAgentGroup;
    private SimpleMultiAgentGroup preyAgentGroup;

    #endregion

    void Start()
    {
        commBuffer.SetCount(hunterCount, preyCount);
        roomGenerator.Generate();
        SpawnAgents();
        Invoke(nameof(ReleaseHunters), hidingTime);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > timeLimit)
        {
            EndEpisode(true); // Temps écoulé, les proies gagnent
        }
    }

    public float GetTimeFloat()
    {
        return (timeLimit - timer) / timeLimit;
    }

    /// <summary>
    /// Instancie les agents chasseurs et proies et les enregistre dans leurs groupes respectifs.
    /// </summary>
    void SpawnAgents()
    {
        hunters = new List<HunterAgentFinal>();
        preys = new List<PreyAgentFinal>();
        disabledHunters = new List<HunterAgentFinal>();
        disabledPreys = new List<PreyAgentFinal>();
        energyList = new List<GameObject>();
        speedBoostList = new List<GameObject>();
        camouflageList = new List<GameObject>();

        GameObject tmp;
        HunterAgentFinal hAgent;
        PreyAgentFinal pAgent;
        Vector3 position;

        List<Vector3> positons = roomGenerator.GetAvailableWorldPositions(preyCount + hunterCount + energyPreyCount + speedBoostCount + camouflageCount);

        // Spawn des chasseurs
        for (int i = 0; i < hunterCount; i++)
        {
            position = this.transform.position + positons[i];
            tmp = Instantiate(hunterPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),this.transform);
            hAgent = tmp.GetComponent<HunterAgentFinal>();
            hAgent.SetEnv(this);
            hunters.Add(hAgent);
        }

        // Spawn des proies
        for (int i = 0; i < preyCount; i++)
        {
            position = this.transform.position + positons[i+hunterCount];
            tmp = Instantiate(preyPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),this.transform);
            pAgent = tmp.GetComponent<PreyAgentFinal>();
            pAgent.SetEnv(this);
            preys.Add(pAgent);
        }

        // spawn energies
        for(int i = 0;i < energyPreyCount; i++)
        {
            position = positons[i + hunterCount + preyCount];
            tmp = ObjectPool.Instance.GetObject(energyPrey, position, Quaternion.identity, this.transform);
            energyList.Add(tmp);
        }

        // Spawn des bonus
        for (int i = 0; i < speedBoostCount; i++)
        {
            position = positons[i + hunterCount + preyCount + energyPreyCount];
            tmp = ObjectPool.Instance.GetObject(speedBoostPrefab, position, Quaternion.identity, this.transform);
            speedBoostList.Add(tmp);
        }
        for (int i = 0; i < camouflageCount; i++)
        {
            position = positons[i + hunterCount + preyCount + energyPreyCount + speedBoostCount];
            tmp = ObjectPool.Instance.GetObject(camouflagePrefab, position, Quaternion.identity, this.transform);
            camouflageList.Add(tmp);
        }


        // Création des groupes multi-agents
        hunterAgentGroup = new SimpleMultiAgentGroup();
        preyAgentGroup = new SimpleMultiAgentGroup();

        foreach (var hunter in hunters)
        {
            hunterAgentGroup.RegisterAgent(hunter);
        }

        foreach (var prey in preys)
        {
            preyAgentGroup.RegisterAgent(prey);
        }
    }

    /// <summary>
    /// Arrête tous les chasseurs en les désactivant temporairement.
    /// </summary>
    void StopHunters()
    {
        huntersReleased = false;
        foreach (var hunter in hunters)
            hunter.GetComponent<HunterAgentFinal>().Deactivate();
    }

    /// <summary>
    /// Active les chasseurs après la période de cachette initiale.
    /// </summary>
    void ReleaseHunters()
    {
        huntersReleased = true;
        foreach (var hunter in hunters)
            hunter.GetComponent<HunterAgentFinal>().Activate();
    }

    /// <summary>
    /// Appelée lorsqu'une proie est attrapée par un chasseur.
    /// </summary>
    /// <param name="prey">La proie attrapée (GameObject)</param>
    public void OnPreyCaught(GameObject prey)
    {
        PreyAgentFinal preyAgent = prey.GetComponent<PreyAgentFinal>();

        if (preys.Contains(preyAgent))
        {
            ObjectPool.Instance.GetObject(diePrefabPrey, prey.transform.position, Quaternion.identity);

            preyAgent.SetAgentDead();

            preys.Remove(preyAgent);
            disabledPreys.Add(preyAgent);

            CheckEndedGame();
        }      
    }

    public void OnPreyEnergy(GameObject energyObj)
    {
        if (energyList.Contains(energyObj))
        {
            StartCoroutine(RespawnBonus(energyObj, delayEnergyRespawn));
        }
    }

    public void OnSpeedBoostCollected(GameObject obj)
    {
        if (speedBoostList.Contains(obj))
        {
            StartCoroutine(RespawnBonus(obj,delaySpeedBonusRespawn));
        }
    }
    public void OnCamouflageCollected(GameObject obj)
    {
        if (camouflageList.Contains(obj))
        {
            StartCoroutine(RespawnBonus(obj,delayCamouflageRespawn));
        }
    }

    private IEnumerator RespawnBonus(GameObject obj, float delay)
    {
        obj.SetActive(false);
        yield return new WaitForSeconds(delay);
        obj.SetActive(true);
    }



    public void OnPreyEnergyDepleted(PreyAgentFinal preyAgent)
    {
        if (preys.Contains(preyAgent))
        {
            ObjectPool.Instance.GetObject(diePrefabPrey, preyAgent.transform.position, Quaternion.identity);

            preyAgent.SetAgentDead();

            preys.Remove(preyAgent);
            disabledPreys.Add(preyAgent);

            CheckEndedGame();
        }

    }

    public void OnPreyEnterDanger(PreyAgentFinal preyAgent)
    {
        if (preys.Contains(preyAgent))
        {
            ObjectPool.Instance.GetObject(diePrefabPrey, preyAgent.transform.position, Quaternion.identity);

            preyAgent.SetAgentDead();

            preys.Remove(preyAgent);
            disabledPreys.Add(preyAgent);

            CheckEndedGame();
        }      
    }



    public void OnHunterEnterDanger(HunterAgentFinal hunterAgent)
    {
        if (hunters.Contains(hunterAgent))
        {
            ObjectPool.Instance.GetObject(diePrefabHunter, hunterAgent.transform.position, Quaternion.identity);

            hunterAgent.SetAgentDead();

            hunters.Remove(hunterAgent);
            disabledHunters.Add(hunterAgent);

            CheckEndedGame();
        }
    }

    public void OnBonusDestroyed(GameObject bonus)
    {
        if (speedBoostList.Contains(bonus))
        {
            StartCoroutine(RespawnBonus(bonus, delaySpeedBonusRespawn));
        }
        else if (camouflageList.Contains(bonus))
        {
            StartCoroutine(RespawnBonus(bonus, delayCamouflageRespawn));
        }

        bonus.SetActive(false); // destruction visuelle immédiate
    }

    void CheckEndedGame()
    {
        if (preys.Count == 0)
        {
            // Toutes les proies sont attrapées → les chasseurs gagnent
            EndEpisode(false);
        }
        else if(hunters.Count == 0)
        {
            // Tous les chasseurs sont morts → les proies gagnent
            EndEpisode(true);
        }
        else
        {
            // Récompense partielle pour les chasseurs, punition pour les proies
            hunterAgentGroup.AddGroupReward(1);
            preyAgentGroup.AddGroupReward(-1);
        }
    }

    /// <summary>
    /// Termine l'épisode, attribue les récompenses finales et redémarre l'environnement.
    /// </summary>
    /// <param name="timeExpired">Si vrai, l’épisode se termine par dépassement du temps</param>
    void EndEpisode(bool timeExpired)
    {
        StopAllCoroutines();

        if (timeExpired)
        {
            hunterAgentGroup.SetGroupReward(-1);
            preyAgentGroup.SetGroupReward(1);
            gameUI.PreyWin();
        }
        else
        {
            hunterAgentGroup.SetGroupReward(1);
            preyAgentGroup.SetGroupReward(-1);
            gameUI.HunterWin();
        }

        hunterAgentGroup.EndGroupEpisode();
        preyAgentGroup.EndGroupEpisode();

        ResetEnv();
    }

    /// <summary>
    /// Réinitialise l'environnement pour un nouvel épisode.
    /// </summary>
    private void ResetEnv()
    {
        commBuffer.ClearDic();
        ObjectPool.Instance.ReturnObjects(energyList);
        ObjectPool.Instance.ReturnObjects(camouflageList);
        ObjectPool.Instance.ReturnObjects(speedBoostList);
        camouflageList.Clear();
        speedBoostList.Clear();
        energyList.Clear();
        

        roomGenerator.Generate();

        timer = 0f;

        // Réactiver les chasseurs désactivés
        foreach (var hunter in disabledHunters)
        {
            hunters.Add(hunter);
            hunter.SetAgentAlive();

            Rigidbody hunterRb = hunter.GetComponent<Rigidbody>();
            if (hunterRb != null)
            {
                hunterRb.linearVelocity = Vector3.zero;
                hunterRb.angularVelocity = Vector3.zero;
                hunterRb.Sleep();
            }
        }
        disabledHunters.Clear();

        // Réactiver les proies désactivées
        foreach (var prey in disabledPreys)
        {
            preys.Add(prey);
            prey.SetAgentAlive();

            Rigidbody preyRb = prey.GetComponent<Rigidbody>();
            if (preyRb != null)
            {
                preyRb.linearVelocity = Vector3.zero;
                preyRb.angularVelocity = Vector3.zero;
                preyRb.Sleep();
            }
        }
        disabledPreys.Clear();

        // Désactive temporairement les chasseurs
        StopHunters();

        List<Vector3> positons = roomGenerator.GetAvailableWorldPositions(preyCount + hunterCount + energyPreyCount + camouflageCount + speedBoostCount);

        Vector3 position;

        // Réinitialiser la position des chasseurs
        for (int i = 0; i < hunters.Count; i++)
        {
            position = this.transform.position + positons[i];
            hunters[i].transform.position = position;
            hunters[i].transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            Rigidbody hunterRb = hunters[i].GetComponent<Rigidbody>();
            if (hunterRb != null)
            {
                hunterRb.linearVelocity = Vector3.zero;
                hunterRb.angularVelocity = Vector3.zero;
                hunterRb.Sleep();
            }
        }

        // Réinitialiser la position des proies
        for (int i = 0; i < preys.Count; i++)
        {
            position = this.transform.position + positons[i + hunterCount];
            preys[i].transform.position = position;
            preys[i].transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            Rigidbody preyRb = preys[i].GetComponent<Rigidbody>();
            if (preyRb != null)
            {
                preyRb.linearVelocity = Vector3.zero;
                preyRb.angularVelocity = Vector3.zero;
                preyRb.Sleep();
            }
        }

        for (int i = 0; i < energyPreyCount; i++)
        {
            position = positons[i + hunterCount + preyCount];      
            energyList.Add(ObjectPool.Instance.GetObject(energyPrey, position, Quaternion.identity, this.transform));
        }


        // Spawn des bonus
        for (int i = 0; i < speedBoostCount; i++)
        {
            position = positons[i + hunterCount + preyCount + energyPreyCount];
            speedBoostList.Add(ObjectPool.Instance.GetObject(speedBoostPrefab, position, Quaternion.identity, this.transform));
        }
        for (int i = 0; i < camouflageCount; i++)
        {
            position = positons[i + hunterCount + preyCount + energyPreyCount + speedBoostCount];
            camouflageList.Add(ObjectPool.Instance.GetObject(camouflagePrefab, position, Quaternion.identity, this.transform));
        }

        // Relancer le compte à rebours pour libérer les chasseurs
        Invoke(nameof(ReleaseHunters), hidingTime);
    }
}
