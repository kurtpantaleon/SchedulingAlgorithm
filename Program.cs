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

        Console.WriteLine("\nEnter Priorities (Lower number = Higher priority): ");
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
        EvaluateAlgorithm("Priority Scheduling (Non-Preemptive)", processes, scheduler => scheduler.PriorityScheduling(), results);
        EvaluateAlgorithm("Shortest Remaining Time First (SRTF)", processes, scheduler => scheduler.SRTF(), results);
        EvaluateAlgorithm($"Round Robin (Quantum = {quantumTime})", processes, scheduler => scheduler.RoundRobin(quantumTime), results);
        EvaluateAlgorithm("Priority Preemptive Scheduling", processes, scheduler => scheduler.PriorityPreemptive(), results);

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
        ganttChart.Clear();
        timeSlices.Clear();
        int currentTime = 0;
        var sorted = processes.OrderBy(p => p.ArrivalTime).ToList();

        foreach (var p in sorted)
        {
            if (currentTime < p.ArrivalTime)
                currentTime = p.ArrivalTime;

            p.WaitingTime = currentTime - p.ArrivalTime;
            p.CompletionTime = currentTime + p.BurstTime;
            p.TurnaroundTime = p.CompletionTime - p.ArrivalTime;
            currentTime += p.BurstTime;
            ganttChart.Add(p.Id);
            timeSlices.Add(p.BurstTime);
        }
    }

    public void SJF()
    {
        ganttChart.Clear();
        timeSlices.Clear();
        int currentTime = 0;
        var remaining = new List<Process>(processes);

        while (remaining.Count > 0)
        {
            var available = remaining.Where(p => p.ArrivalTime <= currentTime).OrderBy(p => p.BurstTime).ThenBy(p => p.ArrivalTime).ToList();
            if (available.Count == 0)
            {
                currentTime++;
                continue;
            }

            var p = available[0];
            p.WaitingTime = currentTime - p.ArrivalTime;
            p.CompletionTime = currentTime + p.BurstTime;
            p.TurnaroundTime = p.CompletionTime - p.ArrivalTime;
            currentTime += p.BurstTime;
            ganttChart.Add(p.Id);
            timeSlices.Add(p.BurstTime);
            remaining.Remove(p);
        }
    }

    public void PriorityScheduling()
    {
        ganttChart.Clear();
        timeSlices.Clear();
        int currentTime = 0;
        var remaining = new List<Process>(processes);

        while (remaining.Count > 0)
        {
            var available = remaining.Where(p => p.ArrivalTime <= currentTime).OrderBy(p => p.Priority).ThenBy(p => p.ArrivalTime).ToList();
            if (available.Count == 0)
            {
                currentTime++;
                continue;
            }

            var p = available[0];
            p.WaitingTime = currentTime - p.ArrivalTime;
            p.CompletionTime = currentTime + p.BurstTime;
            p.TurnaroundTime = p.CompletionTime - p.ArrivalTime;
            currentTime += p.BurstTime;
            ganttChart.Add(p.Id);
            timeSlices.Add(p.BurstTime);
            remaining.Remove(p);
        }
    }

    public void SRTF()
    {
        ganttChart.Clear();
        timeSlices.Clear();
        foreach (var p in processes)
        {
            p.RemainingTime = p.BurstTime;
            p.CompletionTime = 0;
        }

        int currentTime = 0;
        int lastProcessId = -1;
        int timeSlice = 0;
        var remaining = new List<Process>(processes);

        while (remaining.Count > 0)
        {
            var available = remaining.Where(p => p.ArrivalTime <= currentTime).OrderBy(p => p.RemainingTime).ThenBy(p => p.ArrivalTime).ToList();
            if (available.Count == 0)
            {
                currentTime++;
                continue;
            }

            var p = available[0];
            if (lastProcessId != p.Id)
            {
                if (lastProcessId != -1)
                {
                    ganttChart.Add(lastProcessId);
                    timeSlices.Add(timeSlice);
                }
                lastProcessId = p.Id;
                timeSlice = 1;
            }
            else
                timeSlice++;

            p.RemainingTime--;
            if (p.RemainingTime == 0)
            {
                p.CompletionTime = currentTime + 1;
                p.TurnaroundTime = p.CompletionTime - p.ArrivalTime;
                p.WaitingTime = p.TurnaroundTime - p.BurstTime;
                remaining.Remove(p);
            }
            currentTime++;
        }
        if (lastProcessId != -1)
        {
            ganttChart.Add(lastProcessId);
            timeSlices.Add(timeSlice);
        }
    }

    public void RoundRobin(int quantum)
    {
        ganttChart.Clear();
        timeSlices.Clear();
        foreach (var proc in processes)
        {
            proc.RemainingTime = proc.BurstTime;
            proc.CompletionTime = 0;
        }

        int currentTime = 0;
        var remaining = new List<Process>(processes);
        Queue<Process> queue = new Queue<Process>();

        while (remaining.Any() || queue.Any())
        {
            foreach (var remProc in remaining.Where(x => x.ArrivalTime <= currentTime).ToList())
            {
                queue.Enqueue(remProc);
                remaining.Remove(remProc);
            }

            if (queue.Count == 0)
            {
                currentTime++;
                continue;
            }

            var currentProc = queue.Dequeue();
            int execTime = Math.Min(quantum, currentProc.RemainingTime);
            ganttChart.Add(currentProc.Id);
            timeSlices.Add(execTime);
            currentTime += execTime;
            currentProc.RemainingTime -= execTime;

            if (currentProc.RemainingTime == 0)
            {
                currentProc.CompletionTime = currentTime;
                currentProc.TurnaroundTime = currentProc.CompletionTime - currentProc.ArrivalTime;
                currentProc.WaitingTime = currentProc.TurnaroundTime - currentProc.BurstTime;
            }
            else
            {
                foreach (var remProc in remaining.Where(x => x.ArrivalTime <= currentTime).ToList())
                {
                    queue.Enqueue(remProc);
                    remaining.Remove(remProc);
                }
                queue.Enqueue(currentProc);
            }
        }
    }


    public void PriorityPreemptive()
    {
        ganttChart.Clear();
        timeSlices.Clear();
        foreach (var p in processes)
        {
            p.RemainingTime = p.BurstTime;
            p.CompletionTime = 0;
        }

        int currentTime = 0;
        int lastProcessId = -1;
        int timeSlice = 0;
        var remaining = new List<Process>(processes);

        while (remaining.Count > 0)
        {
            var available = remaining.Where(p => p.ArrivalTime <= currentTime).OrderBy(p => p.Priority).ThenBy(p => p.ArrivalTime).ToList();
            if (available.Count == 0)
            {
                currentTime++;
                continue;
            }

            var p = available[0];
            if (lastProcessId != p.Id)
            {
                if (lastProcessId != -1)
                {
                    ganttChart.Add(lastProcessId);
                    timeSlices.Add(timeSlice);
                }
                lastProcessId = p.Id;
                timeSlice = 1;
            }
            else
                timeSlice++;

            p.RemainingTime--;
            if (p.RemainingTime == 0)
            {
                p.CompletionTime = currentTime + 1;
                p.TurnaroundTime = p.CompletionTime - p.ArrivalTime;
                p.WaitingTime = p.TurnaroundTime - p.BurstTime;
                remaining.Remove(p);
            }
            currentTime++;
        }
        if (lastProcessId != -1)
        {
            ganttChart.Add(lastProcessId);
            timeSlices.Add(timeSlice);
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
        foreach (var p in processes)
        {
            Console.WriteLine($"| P{p.Id}          | {p.ArrivalTime,12} | {p.BurstTime,10} | {p.Priority,8} |");
        }
        Console.WriteLine("+-------------+--------------+------------+----------+");
    }

    public void ShowGanttChart(List<int> ganttChart, List<Process> processes, List<int> timeSlices)
    {
        Console.WriteLine("\nGantt Chart");
        Console.Write("|");
        foreach (var id in ganttChart)
        {
            Console.Write($" P{id} |");
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
        foreach (var p in processes)
        {
            Console.WriteLine($"P{p.Id}: TurnAround Time = {p.TurnaroundTime}, Waiting Time = {p.WaitingTime}");
        }
        double avgTAT = processes.Average(p => p.TurnaroundTime);
        double avgWT = processes.Average(p => p.WaitingTime);
        Console.WriteLine($"\nAverage Turnaround Time: {avgTAT:F2}");
        Console.WriteLine($"Average Waiting Time: {avgWT:F2}");
    }
}
