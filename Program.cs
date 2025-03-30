using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        int n;
        while (true)
        {
            Console.Write("Enter the number of processes: ");
            if (int.TryParse(Console.ReadLine(), out n) && n > 0)
                break;
            Console.WriteLine("Invalid input! Please enter a positive integer.");
        }

        List<Process> processes = new List<Process>();

        Console.WriteLine("\nEnter Arrival Times: ");
        List<int> arrivalTimes = LimitedIntegerInput(n);

        Console.WriteLine("\nEnter Burst Times: ");
        List<int> burstTimes = LimitedIntegerInput(n);

        Console.WriteLine("\nEnter Priorities: ");
        List<int> priorities = LimitedIntegerInput(n);

        Console.WriteLine("\nEnter Quantum Time for Round Robin: ");
        List<int> quantumInput = LimitedIntegerInput(1);
        int quantumTime = quantumInput[0];

        for (int i = 0; i < n; i++)
        {
            processes.Add(new Process
            {
                Id = i + 1,
                ArrivalTime = arrivalTimes[i],
                BurstTime = burstTimes[i],
                RemainingTime = burstTimes[i],
                Priority = priorities[i]
            });
        }

        var display = new DisplayManager();
        display.ShowCPUSchedulingTable(processes);

        var results = new Dictionary<string, (List<Process> Processes, List<int> GanttChart, List<int> TimeSlices, double ShowTimes)>();

        EvaluateAlgorithm("First Come First Serve (FCFS)", processes, scheduler => scheduler.FCFS(), results);
        EvaluateAlgorithm("Shortest Job First (SJF)", processes, scheduler => scheduler.SJF(), results);
        EvaluateAlgorithm("Priority Scheduling", processes, scheduler => scheduler.PriorityScheduling(), results);
        EvaluateAlgorithm("Shortest Remaining Time First (SRTF)", processes, scheduler => scheduler.SRTF(), results);
        EvaluateAlgorithm($"Round Robin (Quantum = {quantumTime})", processes, scheduler => scheduler.RoundRobin(quantumTime), results);

        double minShowTimes = results.Min(r => r.Value.ShowTimes);
        var bestAlgorithms = results.Where(r => r.Value.ShowTimes == minShowTimes).ToList();

        Console.WriteLine("\nBest Algorithm(s) with shortest showTimes:");
        foreach (var algo in bestAlgorithms)
        {
            Console.WriteLine($"\nAlgorithm: {algo.Key}");
            display.ShowGanttChart(algo.Value.GanttChart, algo.Value.Processes, algo.Value.TimeSlices);
            display.ShowTimes(algo.Value.Processes.OrderBy(p => p.Id).ToList());
        }
    }

    static void EvaluateAlgorithm(string name, List<Process> processes,
        Action<ProcessScheduler> action,
        Dictionary<string, (List<Process>, List<int>, List<int>, double)> results)
    {
        var scheduler = new ProcessScheduler(processes);
        action(scheduler);
        var finishedProcesses = scheduler.GetProcesses();
        double showTimes = finishedProcesses.Sum(p => p.TurnaroundTime + p.WaitingTime);
        results[name] = (finishedProcesses, scheduler.GetGanttChart(), scheduler.GetTimeSlices(), showTimes);
    }

    static List<int> LimitedIntegerInput(int count)
    {
        List<int> numbers = new List<int>();
        string currentNumber = "";

        while (numbers.Count < count)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Enter && !string.IsNullOrEmpty(currentNumber))
            {
                numbers.Add(int.Parse(currentNumber));
                Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (currentNumber.Length > 0)
                {
                    currentNumber = currentNumber.Substring(0, currentNumber.Length - 1);
                    Console.Write("\b \b");
                }
                else if (numbers.Count > 0)
                {
                    currentNumber = numbers.Last().ToString();
                    numbers.RemoveAt(numbers.Count - 1);
                    Console.Write("\b \b\b \b");
                }
            }
            else if (char.IsDigit(key.KeyChar))
            {
                if (currentNumber.Length < 3)
                {
                    currentNumber += key.KeyChar;
                    Console.Write(key.KeyChar);
                }
            }
            else if (key.Key == ConsoleKey.Spacebar && !string.IsNullOrEmpty(currentNumber))
            {
                numbers.Add(int.Parse(currentNumber));
                currentNumber = "";
                Console.Write(" ");
            }
        }

        return numbers;
    }
}

public class Process
{
    public int Id { get; set; }
    public int ArrivalTime { get; set; }
    public int BurstTime { get; set; }
    public int RemainingTime { get; set; }
    public int Priority { get; set; }
    public int WaitingTime { get; set; }
    public int TurnaroundTime { get; set; }
    public int CompletionTime { get; set; }
}

public class ProcessScheduler
{
    private List<Process> processes;
    private List<int> ganttChart;
    private List<int> timeSlices;

    public ProcessScheduler(List<Process> processes)
    {
        this.processes = processes.Select(p => new Process
        {
            Id = p.Id,
            ArrivalTime = p.ArrivalTime,
            BurstTime = p.BurstTime,
            RemainingTime = p.BurstTime,
            Priority = p.Priority
        }).ToList();
        this.ganttChart = new List<int>();
        this.timeSlices = new List<int>();
    }

    public void FCFS()
    {
        var sortedProcesses = processes.OrderBy(p => p.ArrivalTime).ToList();
        int currentTime = 0;
        ganttChart.Clear();
        timeSlices.Clear();

        foreach (var process in sortedProcesses)
        {
            if (currentTime < process.ArrivalTime)
                currentTime = process.ArrivalTime;

            process.WaitingTime = currentTime - process.ArrivalTime;
            process.CompletionTime = currentTime + process.BurstTime;
            process.TurnaroundTime = process.CompletionTime - process.ArrivalTime;
            currentTime += process.BurstTime;
            ganttChart.Add(process.Id);
            timeSlices.Add(process.BurstTime);
        }
    }

    public void SJF()
    {
        int currentTime = 0;
        ganttChart.Clear();
        timeSlices.Clear();
        var remainingProcesses = new List<Process>(processes);

        while (remainingProcesses.Count > 0)
        {
            var availableProcesses = remainingProcesses
                .Where(p => p.ArrivalTime <= currentTime)
                .OrderBy(p => p.BurstTime)
                .ThenBy(p => p.ArrivalTime)
                .ToList();

            if (availableProcesses.Count == 0)
            {
                int nextArrival = remainingProcesses.Min(p => p.ArrivalTime);
                currentTime = nextArrival;
                continue;
            }

            var selectedProcess = availableProcesses[0];
            selectedProcess.WaitingTime = currentTime - selectedProcess.ArrivalTime;
            selectedProcess.CompletionTime = currentTime + selectedProcess.BurstTime;
            selectedProcess.TurnaroundTime = selectedProcess.CompletionTime - selectedProcess.ArrivalTime;
            currentTime += selectedProcess.BurstTime;
            ganttChart.Add(selectedProcess.Id);
            timeSlices.Add(selectedProcess.BurstTime);
            remainingProcesses.Remove(selectedProcess);
        }
    }

    public void PriorityScheduling()
    {
        int currentTime = 0;
        ganttChart.Clear();
        timeSlices.Clear();
        var remainingProcesses = new List<Process>(processes);

        while (remainingProcesses.Count > 0)
        {
            var availableProcesses = remainingProcesses
                .Where(p => p.ArrivalTime <= currentTime)
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.ArrivalTime)
                .ToList();

            if (availableProcesses.Count == 0)
            {
                int nextArrival = remainingProcesses.Min(p => p.ArrivalTime);
                currentTime = nextArrival;
                continue;
            }

            var selectedProcess = availableProcesses[0];
            selectedProcess.WaitingTime = currentTime - selectedProcess.ArrivalTime;
            selectedProcess.CompletionTime = currentTime + selectedProcess.BurstTime;
            selectedProcess.TurnaroundTime = selectedProcess.CompletionTime - selectedProcess.ArrivalTime;
            currentTime += selectedProcess.BurstTime;
            ganttChart.Add(selectedProcess.Id);
            timeSlices.Add(selectedProcess.BurstTime);
            remainingProcesses.Remove(selectedProcess);
        }
    }

    public void SRTF()
    {
        ganttChart.Clear();
        timeSlices.Clear();

        foreach (var process in processes)
        {
            process.RemainingTime = process.BurstTime;
            process.CompletionTime = 0;
        }

        int currentTime = 0;
        int lastProcessId = -1;
        int currentTimeSlice = 0;

        var remainingProcesses = new List<Process>(processes);

        while (remainingProcesses.Count > 0)
        {
            var availableProcesses = remainingProcesses
                .Where(p => p.ArrivalTime <= currentTime)
                .OrderBy(p => p.RemainingTime)
                .ThenBy(p => p.ArrivalTime)
                .ToList();

            if (availableProcesses.Count == 0)
            {
                int nextArrival = remainingProcesses.Min(p => p.ArrivalTime);
                currentTime = nextArrival;
                continue;
            }

            var selectedProcess = availableProcesses[0];

            if (lastProcessId != selectedProcess.Id)
            {
                if (lastProcessId != -1)
                {
                    ganttChart.Add(lastProcessId);
                    timeSlices.Add(currentTimeSlice);
                }
                lastProcessId = selectedProcess.Id;
                currentTimeSlice = 1;
            }
            else
            {
                currentTimeSlice++;
            }

            selectedProcess.RemainingTime--;

            if (selectedProcess.RemainingTime == 0)
            {
                selectedProcess.CompletionTime = currentTime + 1;
                selectedProcess.TurnaroundTime = selectedProcess.CompletionTime - selectedProcess.ArrivalTime;
                selectedProcess.WaitingTime = selectedProcess.TurnaroundTime - selectedProcess.BurstTime;
                remainingProcesses.Remove(selectedProcess);
            }

            currentTime++;
        }

        if (lastProcessId != -1)
        {
            ganttChart.Add(lastProcessId);
            timeSlices.Add(currentTimeSlice);
        }
    }

    public void RoundRobin(int quantum)
    {
        ganttChart.Clear();
        timeSlices.Clear();
        foreach (var process in processes)
        {
            process.RemainingTime = process.BurstTime;
            process.CompletionTime = 0;
        }

        int currentTime = 0;
        var remainingProcesses = new List<Process>(processes);
        Queue<Process> queue = new Queue<Process>();

        while (remainingProcesses.Any() || queue.Any())
        {
            foreach (var p in remainingProcesses.Where(p => p.ArrivalTime <= currentTime).ToList())
            {
                queue.Enqueue(p);
                remainingProcesses.Remove(p);
            }

            if (queue.Count == 0)
            {
                currentTime++;
                continue;
            }

            var currentProcess = queue.Dequeue();
            int executionTime = Math.Min(quantum, currentProcess.RemainingTime);
            ganttChart.Add(currentProcess.Id);
            timeSlices.Add(executionTime);

            currentTime += executionTime;
            currentProcess.RemainingTime -= executionTime;

            if (currentProcess.RemainingTime == 0)
            {
                currentProcess.CompletionTime = currentTime;
                currentProcess.TurnaroundTime = currentProcess.CompletionTime - currentProcess.ArrivalTime;
                currentProcess.WaitingTime = currentProcess.TurnaroundTime - currentProcess.BurstTime;
            }
            else
            {
                foreach (var p in remainingProcesses.Where(p => p.ArrivalTime <= currentTime).ToList())
                {
                    queue.Enqueue(p);
                    remainingProcesses.Remove(p);
                }
                queue.Enqueue(currentProcess);
            }
        }
    }

    public List<Process> GetProcesses()
    {
        return processes;
    }

    public List<int> GetGanttChart()
    {
        return ganttChart;
    }

    public List<int> GetTimeSlices()
    {
        return timeSlices;
    }
}

public class DisplayManager
{
    public void ShowCPUSchedulingTable(List<Process> processes)
    {
        Console.WriteLine("\nCPU Scheduling Table");
        Console.WriteLine("+-------------+--------------+------------+----------+");
        Console.WriteLine("| Process No  | Arrival Time | Burst Time | Priority |");
        Console.WriteLine("+-------------+--------------+------------+----------+");
        foreach (var process in processes)
        {
            Console.WriteLine($"| P{process.Id}          | {process.ArrivalTime,12} | {process.BurstTime,10} | {process.Priority,8} |");
        }
        Console.WriteLine("+-------------+--------------+------------+----------+");
    }

    public void ShowGanttChart(List<int> ganttChart, List<Process> processes, List<int> timeSlices)
    {
        if (ganttChart.Count == 0)
        {
            Console.WriteLine("\nNo processes scheduled.");
            return;
        }

        Console.WriteLine("\nGantt Chart");
        Console.Write("|");
        foreach (var processId in ganttChart)
        {
            Console.Write($" P{processId} |");
        }
        Console.WriteLine("\n" + new string('-', ganttChart.Count * 5 + 1));

        int currentTime = 0;
        Console.Write(currentTime);

        for (int i = 0; i < ganttChart.Count; i++)
        {
            currentTime += timeSlices[i];
            Console.Write($"{new string(' ', 5)}{currentTime}");
        }
        Console.WriteLine();
    }

    public void ShowTimes(List<Process> processes)
    {
        Console.WriteLine("\nTurnaround Time & Waiting Time:");
        foreach (var process in processes)
        {
            Console.WriteLine($"P{process.Id}: TurnAround Time = {process.TurnaroundTime}, Waiting Time = {process.WaitingTime}");
        }
        double avgTurnaround = processes.Average(p => p.TurnaroundTime);
        double avgWaiting = processes.Average(p => p.WaitingTime);
        Console.WriteLine($"\nAverage Turnaround Time: {avgTurnaround:F2}");
        Console.WriteLine($"Average Waiting Time: {avgWaiting:F2}");

    }
}
