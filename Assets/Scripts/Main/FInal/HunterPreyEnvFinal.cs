using UnityEngine;
using System.Collections.Generic;
using Unity.MLAgents;
using System.Collections;

/// <summary>
/// Gère l'environnement de jeu pour un scénario de chasse entre chasseurs et proies.
/// Contrôle l'apparition, les rewards de group, la réinitialisation et le déroulement des épisodes.
/// </summary>
public class HunterPreyEnvFinal : MonoBehaviour
{
    #region Variables

    // === [Header] Prefabs & Spawn ===

    [Header("Prefabs & Spawn")]
    public GameObject hunterPrefab;
    public GameObject preyPrefab;
    public GameObject diePrefabHunter;
    public GameObject diePrefabPrey;
    public GameObject energyPrey;
    public GameObject speedBoostPrefab;
    public GameObject camouflagePrefab;

    // === [Header] Paramètres de jeu ===

    [Header("Game Settings")]
    public int hunterCount = 2;
    public int preyCount = 3;
    public float timeLimit = 30f;

    public int energyPreyCount = 3;
    public int speedBoostCount = 2;
    public int camouflageCount = 2;

    public float delayEnergyRespawn = 5f;
    public float delaySpeedBonusRespawn = 7f;
    public float delayCamouflageRespawn = 8f;

    // === Gestion des objets de jeu ===

    private List<GameObject> speedBoostList;
    private List<GameObject> camouflageList;
    private List<GameObject> energyList;

    // === Générateur de niveau ===

    public RoomGenerator2D roomGenerator;

    // === UI et communication ===

    public InGameUI gameUI;
    public CommunicationBuffer commBuffer;

    // === Gestion du temps et du jeu ===

    private float hidingTime = 5f;
    private bool huntersReleased = false;
    private float timer;

    // === Groupes d’agents ===

    private SimpleMultiAgentGroup hunterAgentGroup;
    private SimpleMultiAgentGroup preyAgentGroup;

    // === Références aux agents ===

    private List<HunterAgentFinal> hunters;
    private List<PreyAgentFinal> preys;
    private List<HunterAgentFinal> disabledHunters;
    private List<PreyAgentFinal> disabledPreys;

    // === Statistiques ===

    private int preyKilledByHunter = 0;

    #endregion

    #region Unity Func

    /// <summary>
    /// Recupere tous les chasseurs.
    /// </summary>
    /// <returns>Liste de HunterAgentFinal</returns>
    public List<HunterAgentFinal>  GetAllHunters()
    {
        return hunters;
    }

    /// <summary>
    /// Recupere toutes les proies.
    /// </summary>
    /// <returns>Liste de PreyAgentFinal</returns>
    public List<PreyAgentFinal> GetAllPreys()
    {
        return preys;
    }

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

    /// <summary>
    /// Recupere le temps actuel normalisé entre 0 et 1.
    /// </summary>
    /// <returns>Le temps actuel</returns>
    public float GetTimeFloat()
    {
        return (timeLimit - timer) / timeLimit;
    }

    private void FixedUpdate()
    {
        preyAgentGroup.AddGroupReward(0.001f * GetTimeFloat());
        if (huntersReleased)
        {
            hunterAgentGroup.AddGroupReward(-0.001f * GetTimeFloat());
        }
    }

    #endregion

    #region Spawn & Rest

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
    /// Vérifie si la partie est finie.
    /// </summary>
    void CheckEndedGame()
    {
        if (preys.Count == 0)
        {
            // Toutes les proies sont attrapées → les chasseurs gagnent
            EndEpisode(false);
        }
        else if (hunters.Count == 0)
        {
            // Tous les chasseurs sont morts → les proies gagnent
            EndEpisode(true);
        }
    }

    /// <summary>
    /// Termine l'épisode, attribue les récompenses finales et redémarre l'environnement.
    /// </summary>
    /// <param name="timeExpired">Si vrai, l’épisode se termine par dépassement du temps</param>
    void EndEpisode(bool timeExpired)
    {
        StopAllCoroutines();

        if (timeExpired && hunters.Count != 0)
        {
            //foreach(var prey in preys)
            //{
            //    prey.SetWin();
            //}
            hunterAgentGroup.SetGroupReward(-1);
            preyAgentGroup.SetGroupReward((float)preys.Count / (float)preyCount);
            gameUI.PreyWin();
        }
        else if (hunters.Count == 0)
        {
            hunterAgentGroup.SetGroupReward(-1);
            preyAgentGroup.SetGroupReward(0.5f);
            gameUI.PreyWin();
        }
        else
        {
            hunterAgentGroup.SetGroupReward((1 / (preyCount + 1)) * (preyKilledByHunter + 1));
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
        preyKilledByHunter = 0;
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

    #endregion

    #region Agent Func

    /// <summary>
    /// Appelée lorsqu'une proie est attrapée par un chasseur.
    /// Gère la mort de la proie, les récompenses/punitions et vérifie si la partie est terminée.
    /// </summary>
    /// <param name="prey">La proie attrapée (GameObject)</param>
    public void OnPreyCaught(GameObject prey)
    {
        PreyAgentFinal preyAgent = prey.GetComponent<PreyAgentFinal>();

        if (preys.Contains(preyAgent))
        {
            preyAgent.AddReward(-10f);

            ObjectPool.Instance.GetObject(diePrefabPrey, prey.transform.position, Quaternion.identity);

            preyAgent.SetAgentDead();

            preys.Remove(preyAgent);
            disabledPreys.Add(preyAgent);

            // Récompense partielle pour les chasseurs, punition pour les proies
            hunterAgentGroup.AddGroupReward(1);
            preyAgentGroup.AddGroupReward(-1);

            preyKilledByHunter++;

            CheckEndedGame();
        }      
    }

    /// <summary>
    /// Appelée lorsqu'une proie collecte une source d'énergie.
    /// Récompense les proies et relance le bonus après un délai.
    /// </summary>
    /// <param name="energyObj">Objet énergie collecté</param>
    public void OnPreyEnergy(GameObject energyObj)
    {
        if (energyList.Contains(energyObj))
        {
            preyAgentGroup.AddGroupReward(1f);
            StartCoroutine(RespawnBonus(energyObj, delayEnergyRespawn));
        }
    }

    /// <summary>
    /// Appelée lorsqu'une proie collecte un bonus de vitesse.
    /// Récompense les proies et relance le bonus après un délai.
    /// </summary>
    /// <param name="obj">Objet vitesse collecté</param>
    public void OnSpeedBoostCollected(GameObject obj)
    {
        if (speedBoostList.Contains(obj))
        {
            preyAgentGroup.AddGroupReward(1f);
            StartCoroutine(RespawnBonus(obj,delaySpeedBonusRespawn));
        }
    }

    /// <summary>
    /// Appelée lorsqu'une proie collecte un camouflage.
    /// Récompense les proies et relance le bonus après un délai.
    /// </summary>
    /// <param name="obj">Objet camouflage collecté</param>
    public void OnCamouflageCollected(GameObject obj)
    {
        if (camouflageList.Contains(obj))
        {
            preyAgentGroup.AddGroupReward(1f);
            StartCoroutine(RespawnBonus(obj,delayCamouflageRespawn));
        }
    }

    /// <summary>
    /// Coroutine utilisée pour réactiver un bonus après un certain délai.
    /// </summary>
    /// <param name="obj">Objet à réactiver</param>
    /// <param name="delay">Temps d’attente avant la réactivation</param>
    private IEnumerator RespawnBonus(GameObject obj, float delay)
    {
        obj.SetActive(false);
        yield return new WaitForSeconds(delay);
        obj.SetActive(true);
    }

    /// <summary>
    /// Appelée lorsqu'une proie n’a plus d’énergie.
    /// Gère la mort de la proie et vérifie si la partie est terminée.
    /// </summary>
    /// <param name="preyAgent">Proie concernée</param>
    public void OnPreyEnergyDepleted(PreyAgentFinal preyAgent)
    {
        if (preys.Contains(preyAgent))
        {
            ObjectPool.Instance.GetObject(diePrefabPrey, preyAgent.transform.position, Quaternion.identity);

            preyAgentGroup.AddGroupReward(-1);

            preyAgent.SetAgentDead();

            preys.Remove(preyAgent);
            disabledPreys.Add(preyAgent);          

            CheckEndedGame();
        }

    }

    /// <summary>
    /// Appelée lorsqu'une proie entre dans une zone dangereuse.
    /// Gère la mort immédiate de la proie.
    /// </summary>
    /// <param name="preyAgent">Proie concernée</param>
    public void OnPreyEnterDanger(PreyAgentFinal preyAgent)
    {
        if (preys.Contains(preyAgent))
        {
            ObjectPool.Instance.GetObject(diePrefabPrey, preyAgent.transform.position, Quaternion.identity);

            preyAgentGroup.AddGroupReward(-5);

            preyAgent.SetAgentDead();

            preys.Remove(preyAgent);
            disabledPreys.Add(preyAgent);
     

            CheckEndedGame();
        }      
    }

    /// <summary>
    /// Appelée lorsqu'un chasseur entre dans une zone dangereuse.
    /// Gère la mort immédiate du chasseur.
    /// </summary>
    /// <param name="hunterAgent">Chasseur concerné</param>
    public void OnHunterEnterDanger(HunterAgentFinal hunterAgent)
    {
        if (hunters.Contains(hunterAgent))
        {
            ObjectPool.Instance.GetObject(diePrefabHunter, hunterAgent.transform.position, Quaternion.identity);

            hunterAgentGroup.AddGroupReward(-5);

            hunterAgent.SetAgentDead();

            hunters.Remove(hunterAgent);
            disabledHunters.Add(hunterAgent);           

            CheckEndedGame();
        }
    }


    /// <summary>
    /// Appelée lorsqu’un bonus est détruit (par exemple consommé).
    /// Récompense les chasseurs et relance le bonus après un délai.
    /// </summary>
    /// <param name="bonus">Objet bonus détruit</param>
    public void OnBonusDestroyed(GameObject bonus)
    {
        hunterAgentGroup.AddGroupReward(0.1f);

        if (speedBoostList.Contains(bonus))
        {
            StartCoroutine(RespawnBonus(bonus, delaySpeedBonusRespawn));
        }
        else if (camouflageList.Contains(bonus))
        {
            StartCoroutine(RespawnBonus(bonus, delayCamouflageRespawn));
        }
        else if (energyList.Contains(bonus)) 
        {
            StartCoroutine(RespawnBonus(bonus, delayEnergyRespawn));
        }


        bonus.SetActive(false); // destruction visuelle immédiate
    }

    #endregion

}
