import matplotlib.pyplot as plt
import csv



files = {
    'exp2_ai_difficulty':'(AI)',
    'exp2_baseline_difficulty':'(Baseline)',
    'exp2_discountReduced_difficulty':'(Gamma = 0.2)',
    'exp2_easy_difficulty':'(Easy)',
    'exp2_exploitation_difficulty':'(Exploitation)',
    'exp2_hard_difficulty':'(Hard)'
}

def csvToPlot(file,training):

    file_name = file
    training_clause = training

    Episodes = []

    SurvivalTime = []

    TotalReward = []


    #read files and parse cols into lists
    with open(file_name + '.csv','r') as csvfile:
        plots = csv.reader(csvfile,delimiter = ',')

        #skip header
        next(plots,None)

        for row in plots:
            

            Episodes.append(int(row[0]))

            #truncates the data
            SurvivalTime.append(int(row[1]))
            TotalReward.append(int(row[2]))

           

    #plot survival time

    plt.figure(figsize = (10,6))

    plt.plot(Episodes,SurvivalTime, color = 'y', linestyle = 'dashed', marker = '.', label = 'Survival Time')

    plt.xlabel('Episodes')
    plt.ylabel('Survival Time')
    plt.title('Survival Time vs Episodes ' + training_clause)
    plt.legend()
    plt.grid(True, alpha=0.2)
    plt.savefig('Survival Time vs Episodes ' + training_clause +  '.png',dpi = 150)
    plt.close()



    #plot reward time

    plt.figure(figsize=(10,6))

    plt.plot(Episodes,TotalReward,color = 'b', linestyle = 'solid', label = 'Episodic Reward')

    plt.xlabel('Episodes')
    plt.ylabel('Total Reward')
    plt.title('Total Rewards vs Episodes ' + training_clause)
    plt.legend()
    plt.grid(True, alpha=0.2)
    plt.savefig('Total Rewards vs Episodes ' + training_clause + '.png',dpi = 150)
    plt.close()





def main():

    for filename,traintype in files.items():

        csvToPlot(filename,traintype)
        print('done')





if __name__ == "__main__":
    main()
