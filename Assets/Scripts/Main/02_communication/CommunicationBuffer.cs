using System.Collections.Generic;
using UnityEngine;

public class CommunicationBuffer : MonoBehaviour
{
    public HunterPreyEnv02 env;

    public struct Message
    {
        public float[] rayResults;
        public Vector3 localPosition;
    }

    private Dictionary<string, Message> messageBufferHunter = new Dictionary<string, Message>();
    private Dictionary<string, Message> previousMessagesHunter = new Dictionary<string, Message>();

    private Dictionary<string, Message> messageBufferPrey = new Dictionary<string, Message>();
    private Dictionary<string, Message> previousMessagesPrey = new Dictionary<string, Message>();

    private int countHunter = 0;
    private int countPrey = 0;

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

    private void CheckNewMessage()
    {
        if (countHunter == env.hunterCount)
        {
            UpdateMessages(true);
        }

        if (countPrey == env.preyCount)
        {
            UpdateMessages(false);
        }
    }

    public Dictionary<string, Message> GetAllMessages(bool isHunter)
    {
        if (isHunter)
            return new Dictionary<string, Message>(previousMessagesHunter);
        else
            return new Dictionary<string, Message>(previousMessagesPrey);
    }

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

    public void ClearDic()
    {
        messageBufferHunter.Clear();
        messageBufferPrey.Clear();
        previousMessagesHunter.Clear();
        previousMessagesPrey.Clear();
    }
}
