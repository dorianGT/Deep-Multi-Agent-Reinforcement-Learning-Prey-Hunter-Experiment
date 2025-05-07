using UnityEngine;
using System.Collections.Generic;
using Unity.MLAgents;
using System.Collections;

/// <summary>
/// Gère l'environnement de jeu pour un scénario de chasse entre chasseurs et proies.
/// Contrôle l'apparition, la réinitialisation et le déroulement des épisodes.
/// </summary>
public class HunterPreyEnv02 : MonoBehaviour
{
    #region Variables

    [Header("Prefabs & Spawn")]
    public GameObject hunterPrefab;
    public GameObject preyPrefab;
    public GameObject diePrefabHunter;
    public GameObject diePrefabPrey;
    public GameObject energyPrey;

    [Header("Game Settings")]
    public int hunterCount = 2;
    public int preyCount = 3;
    public float timeLimit = 30f;
    public int energyPreyCount = 3;

    public RoomGenerator2D roomGenerator;

    public InGameUI gameUI;

    private float hidingTime = 5f;
    private bool huntersReleased = false;

    private List<HunterAgent02> hunters;
    private List<PreyAgent02> preys;
    private List<HunterAgent02> disabledHunters;
    private List<PreyAgent02> disabledPreys;

    private List<GameObject> energyList;

    private float timer;

    private SimpleMultiAgentGroup hunterAgentGroup;
    private SimpleMultiAgentGroup preyAgentGroup;

    #endregion

    void Start()
    {
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
    /// Instancie les agents chasseurs et proies et les enregistre dans leurs groupes respectifs.
    /// </summary>
    void SpawnAgents()
    {
        hunters = new List<HunterAgent02>();
        preys = new List<PreyAgent02>();
        disabledHunters = new List<HunterAgent02>();
        disabledPreys = new List<PreyAgent02>();
        energyList = new List<GameObject>();

        GameObject tmp;
        HunterAgent02 hAgent;
        PreyAgent02 pAgent;
        Vector3 position;

        List<Vector3> positons = roomGenerator.GetAvailableWorldPositions(preyCount + hunterCount + energyPreyCount);

        // Spawn des chasseurs
        for (int i = 0; i < hunterCount; i++)
        {
            position = this.transform.position + positons[i];
            tmp = Instantiate(hunterPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),this.transform);
            hAgent = tmp.GetComponent<HunterAgent02>();
            hAgent.SetEnv(this);
            hunters.Add(hAgent);
        }

        // Spawn des proies
        for (int i = 0; i < preyCount; i++)
        {
            position = this.transform.position + positons[i+hunterCount];
            tmp = Instantiate(preyPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),this.transform);
            pAgent = tmp.GetComponent<PreyAgent02>();
            pAgent.SetEnv(this);
            preys.Add(pAgent);
        }

        for(int i = 0;i < energyPreyCount; i++)
        {
            position = positons[i + hunterCount + preyCount];
            tmp = ObjectPool.Instance.GetObject(energyPrey, position, Quaternion.identity, this.transform);
            energyList.Add(tmp);
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
            hunter.GetComponent<HunterAgent02>().Deactivate();
    }

    /// <summary>
    /// Active les chasseurs après la période de cachette initiale.
    /// </summary>
    void ReleaseHunters()
    {
        huntersReleased = true;
        foreach (var hunter in hunters)
            hunter.GetComponent<HunterAgent02>().Activate();
    }

    /// <summary>
    /// Appelée lorsqu'une proie est attrapée par un chasseur.
    /// </summary>
    /// <param name="prey">La proie attrapée (GameObject)</param>
    public void OnPreyCaught(GameObject prey)
    {
        PreyAgent02 preyAgent = prey.GetComponent<PreyAgent02>();

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
            energyList.Remove(energyObj);
            StartCoroutine(RespawnEnergyAtSamePosition(energyObj, 3f));
        }
    }

    private IEnumerator RespawnEnergyAtSamePosition(GameObject energyObj, float delay)
    {
        energyObj.SetActive(false);

        yield return new WaitForSeconds(delay);

        energyObj.SetActive(true);
        energyList.Add(energyObj);
    }



    public void OnPreyEnergyDepleted(PreyAgent02 preyAgent)
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

    public void OnPreyEnterDanger(PreyAgent02 preyAgent)
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

    public void OnHunterEnterDanger(HunterAgent02 hunterAgent)
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
        ObjectPool.Instance.ReturnObjects(energyList);
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

        List<Vector3> positons = roomGenerator.GetAvailableWorldPositions(preyCount + hunterCount + energyPreyCount);

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

        // Relancer le compte à rebours pour libérer les chasseurs
        Invoke(nameof(ReleaseHunters), hidingTime);
    }
}
