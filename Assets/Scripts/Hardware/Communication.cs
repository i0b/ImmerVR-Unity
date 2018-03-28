using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
class CommandList
{
    public Command[] modules;
}

[System.Serializable]
class Command
{
    public int id;
    public int[] values;
}

[System.Serializable]
class ModuleInformation
{
    public int id;
    public string type;
    public List<int> values;
    public List<double> measurements;

    public static ModuleInformation CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<ModuleInformation>(jsonString);
    }
}

[System.Serializable]
class Summary
{
    public List<ModuleInformation> modules;

    public static Summary CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<Summary>(jsonString);
    }
}

public class Communication : MonoBehaviour
{
    public float messageSendRateMs;
    public SerialController serialController;
    public bool ShowInMessages = true;
    public bool ShowOutMessages = true;
    public int[] MessageFilterById;

    private Stack<Command> commandsToSendNext;
    private float sendNextUpdates;
    private Dictionary<int, Module> modulesById;
    private Dictionary<int, Command> commandsById;
    private Dictionary<int, Stack<int[]>> queuedActuatorValuesById;

    void Start()
    {
        commandsToSendNext = new Stack<Command>();
        modulesById = new Dictionary<int, Module>();
        commandsById = new Dictionary<int, Command>();
        queuedActuatorValuesById = new Dictionary<int, Stack<int[]>>();

        Module[] modules = FindObjectsOfType(typeof(Module)) as Module[];

        foreach (Module module in modules)
        {
            modulesById.Add(module.ID, module);
            commandsById.Add(module.ID, new Command { id = module.ID });
            queuedActuatorValuesById.Add(module.ID, new Stack<int[]>());
        }

        sendNextUpdates = 0.0f;

        serialController = GameObject.Find("HardwareController").GetComponent<SerialController>();
    }

    public void QueueValues(int moduleId, int[] values)
    {
        queuedActuatorValuesById[moduleId].Push((int[])values.Clone());
    }

    private string ReceiveMessage()
    {
        if (serialController.enabled == false)
        {
            return null;
        }

        return serialController.ReadSerialMessage();
    }

    private void SendCommands()
    {
        string output = null;

        if (commandsToSendNext.Count == 1)
        {
            Command command = commandsToSendNext.Pop();
            output = JsonUtility.ToJson(command);
        }
        else if (commandsToSendNext.Count > 0)
        {
            CommandList commandList = new CommandList { modules = commandsToSendNext.ToArray() };
            output = JsonUtility.ToJson(commandList);
            commandsToSendNext.Clear();
        }

        if (output != null)
        {
            if (serialController.enabled == true)
            {
                serialController.SendSerialMessage(output);
            }

            if (ShowOutMessages == true)
            {
                Debug.Log(output);
            }

            /*
            bool moduleIdToBeDisplayed = false;

            for (int i = 0; i < MessageFilterById.Length; i++)
            {
                if (MessageFilterById[i] == command.id)
                {
                    moduleIdToBeDisplayed = true;
                    break;
                }
            }

            if (MessageFilterById.Length == 0 || moduleIdToBeDisplayed)
            {
                Debug.Log(JsonUtility.ToJson(command));
            }
            */
        }

    }

    private void SetColor(int moduleId, int[] values)
    {
        Module module = modulesById[moduleId];

        if (module == null)
        {
            Debug.Log("ERROR: module with ID " + moduleId + " not found.");
            return;
        }

        GameObject[] actuators = module.getActuators();

        if (values == null)
        {
            Debug.Log("ERROR: values null");
            return;
        }

        if (values.Length != actuators.Length)
        {
            Debug.Log("ERROR: setting color not possible size of values does not match size actuators");
            return;
        }

        if (module.actuatorType == ActuatorType.Haptic)
        {
            for (int i = 0; i < values.Length; i++)
            {
                float greyValue = (float)values[i] / (float)(module.MaxValue - module.MinValue);

                Color newColor = new Color(greyValue, greyValue, greyValue);

                Renderer renderer = actuators[i].GetComponent<Renderer>();
                renderer.material.color = newColor;
            }
        }

        else if (module.actuatorType == ActuatorType.Temperature)
        {
            for (int i = 0; i < values.Length; i++)
            {
                // values range from 20 - 33
                Color newColor;

                if (values[i] == 0)
                {
                    newColor = Color.black;
                }
                else
                {
                    float blueRed = (float)(values[i] - module.MinValue) / (float)(module.MaxValue - module.MinValue);
                    newColor = new Color(blueRed, 0, 1 - blueRed);
                }

                Renderer renderer = actuators[i].GetComponent<Renderer>();
                renderer.material.color = newColor;
            }
        }

        else if (module.actuatorType == ActuatorType.EMS)
        {
            for (int i = 0; i < values.Length; i++)
            {
                float yellowValue = (float)values[i] / (float)(module.MaxValue - module.MinValue);

                Color newColor = new Color(yellowValue, yellowValue, 0);

                Renderer renderer = actuators[i].GetComponent<Renderer>();
                renderer.material.color = newColor;
            }
        }
    }

    private int[] MinStackValues(Stack<int[]> stack)
    {
        if (stack.Count == 0)
        {
            return null;
        }

        int[] minValues = stack.Pop();

        while (stack.Count > 0)
        {
            int[] currentValues = stack.Pop();

            for (int i = 0; i < minValues.Length; i++)
            {
                if (currentValues[i] < minValues[i])
                {
                    minValues[i] = currentValues[i];
                }
            }
        }

        return minValues;
    }

    private int[] MaxStackValues(Stack<int[]> stack)
    {
        if (stack.Count == 0)
        {
            return null;
        }

        int[] maxValues = stack.Pop();

        while (stack.Count > 0)
        {
            int[] currentValues = stack.Pop();

            for (int i = 0; i < maxValues.Length; i++)
            {
                if (currentValues[i] > maxValues[i])
                {
                    maxValues[i] = currentValues[i];
                }
            }
        }

        return maxValues;
    }

    private int[] AverageStackValues(Stack<int[]> stack)
    {
        int stackSize = stack.Count;

        if (stackSize == 0)
        {
            return null;
        }

        int[] totalValues = stack.Pop();

        while (stack.Count > 0)
        {
            int[] currentValues = stack.Pop();

            for (int i = 0; i < totalValues.Length; i++)
            {
                totalValues[i] = totalValues[i] + currentValues[i];
            }
        }

        // average
        for (int i = 0; i < totalValues.Length; i++)
        {
            totalValues[i] = totalValues[i] / stackSize;
        }

        return totalValues;
    }

    void Update()
    {
        // ---- process commands and send them out if time threshold has been reached ----

        sendNextUpdates += Time.deltaTime;

        if (sendNextUpdates >= messageSendRateMs / 1000)
        {
            // for each module first check if there are new values, if so send them a command over serial
            foreach (KeyValuePair<int, Command> entry in commandsById)
            {
                Command command = entry.Value;
                int moduleId = entry.Key;

                int[] newValues = null;

                switch (modulesById[moduleId].actuatorType)
                {
                    case ActuatorType.Haptic:
                        newValues = MaxStackValues(queuedActuatorValuesById[moduleId]);
                        break;
                    case ActuatorType.Temperature:
                        newValues = MaxStackValues(queuedActuatorValuesById[moduleId]);
                        //newValues = AverageStackValues(queuedActuatorValuesById[moduleId]);
                        break;
                    case ActuatorType.EMS:
                        newValues = MaxStackValues(queuedActuatorValuesById[moduleId]);
                        //newValues = AverageStackValues(queuedActuatorValuesById[moduleId]);
                        break;
                }

                if (newValues != null)
                {
                    command.values = newValues;
                    commandsToSendNext.Push(command);
                    SetColor(moduleId, command.values);

                }
            }

            SendCommands();
            sendNextUpdates = 0.0f;
        }

        // ---- process incomming messages ----

        string message = ReceiveMessage();

        if (message == null)
            return;

        // Check if the message is plain data or a connect/disconnect event.
        if (ReferenceEquals(message, SerialController.SERIAL_DEVICE_CONNECTED))
            Debug.Log("Connection established");
        else if (ReferenceEquals(message, SerialController.SERIAL_DEVICE_DISCONNECTED))
            Debug.Log("Connection attempt failed or disconnection detected");
        else
        {
            if (ShowInMessages == true)
            {
                Debug.Log("Received message: " + message);
            }

            // parse json
            try
            {
                Summary summary = Summary.CreateFromJSON(message);

                foreach (ModuleInformation item in summary.modules)
                {
                    modulesById[item.id].setReadValues(item.values.ToArray());

                    modulesById[item.id].setMeasuredValues(item.measurements.ToArray());
                }

            }
            catch (ArgumentException e)
            {
                Debug.Log("Not valid JSON");
            }
        }
    }
}