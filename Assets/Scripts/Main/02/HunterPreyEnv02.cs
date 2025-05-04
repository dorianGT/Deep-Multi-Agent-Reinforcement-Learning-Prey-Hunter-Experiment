using UnityEngine;
using System.Collections.Generic;
using Unity.MLAgents;

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

    [Header("Game Settings")]
    public int hunterCount = 2;
    public int preyCount = 3;
    public float timeLimit = 30f;

    public RoomGenerator2D roomGenerator;

    private float hidingTime = 5f;
    private bool huntersReleased = false;

    private List<HunterAgent02> hunters;
    private List<PreyAgent02> preys;
    private List<HunterAgent02> disabledHunters;
    private List<PreyAgent02> disabledPreys;

    private float timer;
    private int hunterWins = 0;
    private int preyWins = 0;

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

        GameObject tmp;
        HunterAgent02 hAgent;
        PreyAgent02 pAgent;
        Vector3 position;

        List<Vector3> positons = roomGenerator.GetAvailableWorldPositions(preyCount + hunterCount);

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

            prey.SetActive(false);         

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

            preyAgent.gameObject.SetActive(false);

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

            hunterAgent.gameObject.SetActive(false);

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
        if (timeExpired)
        {
            hunterAgentGroup.SetGroupReward(-1);
            preyAgentGroup.SetGroupReward(1);
            preyWins++;
        }
        else
        {
            hunterAgentGroup.SetGroupReward(1);
            preyAgentGroup.SetGroupReward(-1);
            hunterWins++;
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
        roomGenerator.Generate();

        timer = 0f;

        // Réactiver les chasseurs désactivés
        foreach (var hunter in disabledHunters)
        {
            hunters.Add(hunter);
            hunter.gameObject.SetActive(true);

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
            prey.gameObject.SetActive(true);

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

        List<Vector3> positons = roomGenerator.GetAvailableWorldPositions(preyCount + hunterCount);

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


        // Relancer le compte à rebours pour libérer les chasseurs
        Invoke(nameof(ReleaseHunters), hidingTime);
    }
}
