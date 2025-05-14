using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère un système de buffer pour la communication entre agents (chasseurs et proies).
/// Permet d’envoyer, stocker et récupérer des messages contenant des informations de perception et de position.
/// </summary>
public class CommunicationBuffer : MonoBehaviour
{
    /// <summary>
    /// Représente un message échangé entre agents contenant les résultats de raycasts, la position locale et la rotation.
    /// </summary>
    public struct Message
    {
        public float[] rayResults;
        public Vector3 localPosition;
        public float rotation;
    }

    private Dictionary<string, Message> messageBufferHunter = new Dictionary<string, Message>();
    private Dictionary<string, Message> previousMessagesHunter = new Dictionary<string, Message>();

    private Dictionary<string, Message> messageBufferPrey = new Dictionary<string, Message>();
    private Dictionary<string, Message> previousMessagesPrey = new Dictionary<string, Message>();

    private int countPreyMax;
    private int countHunterMax;

    private int countHunter = 0;
    private int countPrey = 0;

    /// <summary>
    /// Définit le nombre total d'agents chasseurs et proies attendus pour la synchronisation des messages.
    /// </summary>
    /// <param name="countHunter">Le nombre de chasseur</param>
    /// <param name="countPrey">Le nombre de proie</param>
    public void SetCount(int countHunter, int countPrey)
    {
        countHunterMax = countHunter;
        countPreyMax = countPrey;
    }

    /// <summary>
    /// Envoie un message pour un agent donné et déclenche la mise à jour si tous les messages attendus sont reçus.
    /// </summary>
    /// <param name="agentId">L'id de l'agent</param>
    /// <param name="message">Le message à transmettre</param>
    /// <param name="isHunter">True si c'est un chasseur, false sinon</param>
    public void SendMessageInfo(string agentId, Message message, bool isHunter)
    {
        if (isHunter)
        {
            messageBufferHunter[agentId] = message;
            countHunter++;
        }
        else
        {        
            messageBufferPrey[agentId] = message;
            countPrey++;
        }

        CheckNewMessage();
    }

    /// <summary>
    /// Vérifie si tous les messages attendus ont été reçus pour les chasseurs ou les proies,
    /// et déclenche la mise à jour des messages si nécessaire.
    /// </summary>
    private void CheckNewMessage()
    {
        if (countHunter == countHunterMax)
        {
            UpdateMessages(true);
        }

        if (countPrey == countPreyMax)
        {
            UpdateMessages(false);
        }
    }

    /// <summary>
    /// Retourne les derniers messages reçus pour tous les agents du type spécifié (chasseur ou proie).
    /// </summary>
    /// <param name="isHunter">True si c'est un chasseur, false sinon</param>
    /// <returns>Les derniers messages reçus</returns>
    public Dictionary<string, Message> GetAllMessages(bool isHunter)
    {
        if (isHunter)
            return new Dictionary<string, Message>(previousMessagesHunter);
        else
            return new Dictionary<string, Message>(previousMessagesPrey);
    }

    /// <summary>
    /// Met à jour les messages précédents avec ceux actuellement dans le buffer, puis réinitialise le buffer.
    /// </summary>
    /// <param name="isHunter">True si c'est un chasseur, false sinon</param>
    public void UpdateMessages(bool isHunter)
    {
        if (isHunter)
        {
            previousMessagesHunter = new Dictionary<string, Message>(messageBufferHunter);
            messageBufferHunter.Clear();
            countHunter = 0;
        }
        else
        {
            previousMessagesPrey = new Dictionary<string, Message>(messageBufferPrey);
            messageBufferPrey.Clear();
            countPrey = 0;
        }
    }

    /// <summary>
    /// Vide tous les dictionnaires de messages (buffer et historiques).
    /// </summary>
    public void ClearDic()
    {
        messageBufferHunter.Clear();
        messageBufferPrey.Clear();
        previousMessagesHunter.Clear();
        previousMessagesPrey.Clear();
    }
}
