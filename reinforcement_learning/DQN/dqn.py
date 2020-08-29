import torch
import torch.nn as nn
import torch.optim as optim
import torch.nn.functional as F
import torchvision.transforms as T
from replaymemory import ReplayMemory
import random
from collections import namedtuple
from itertools import count
import numpy as np

class NeuralNetwork(nn.Module):
    def __init__(self, input_size, output_size):
        super(NeuralNetwork, self).__init__()
        h1 = nn.Linear(input_size, 20*input_size)
        h2 = nn.Linear(input_size*20,(20*input_size+output_size)//2)
        h3 = nn.Linear((20*input_size+output_size)//2, output_size)
        self.hidden = nn.Sequential(
            h1,
            nn.Tanh(),
            h2,
            nn.Tanh(),
            h3
        )
        asdf= 1
    
    def forward(self, x):
        output = self.hidden(x)
        return output

class DQN():
    def __init__(self, env):
        self.env = env
        self.state_size = env.state_size
        self.action_size = env.action_size
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        self.policy_net = NeuralNetwork(self.state_size, self.action_size).to(self.device)
        self.target_net = NeuralNetwork(self.state_size, self.action_size).to(self.device)
        self.target_net.load_state_dict(self.policy_net.state_dict())
        self.target_net.eval()
        self.epsilon = 1
        self.epsilon_decay = 0.0001
        self.memory = ReplayMemory(1000)
        self.batch_size = 128
        self.optimizer = optim.RMSprop(self.policy_net.parameters())
        self.gamma = 0.99
        self.target_update = 10
        self.score_history = []
    
    def select_action(self, state, train = True):
        if random.random() < self.epsilon or train is False:
            return torch.tensor([[random.randrange(self.action_size)]], device = self.device, dtype = torch.long)
        else:
            with torch.no_grad():
                return self.policy_net(state).max(1)[1].view(1,1)
        
        
    def update_policy_net(self):
        if len(self.memory) < self.batch_size:
            return
        
        batch = self.memory.transited_sample(self.batch_size)
        
        non_final_mask = torch.tensor(tuple(map(lambda s: s is not None, batch.next_state)), device = self.device, dtype = torch.bool)
        
        non_final_next_states = torch.from_numpy(np.asarray([s for s in batch.next_state if s is not None])).to(self.device)
    
        state_batch = torch.from_numpy(np.asarray(batch.state)).to(self.device)
        action_batch = torch.cat(batch.action).to(self.device)
        reward_batch = torch.cat(batch.reward).to(self.device).float()

        state_action_values = self.policy_net(state_batch)
        state_action_values = torch.gather(self.policy_net(state_batch),1, action_batch) #value of Q function

        next_state_values = torch.zeros(self.batch_size, device=self.device)
        next_state_values[non_final_mask] = self.target_net(non_final_next_states).max(1)[0].detach()
        expected_state_action_values = (next_state_values * self.gamma) + reward_batch

        loss = F.smooth_l1_loss(state_action_values, expected_state_action_values.unsqueeze(1))

        self.optimizer.zero_grad()
        loss.backward()
        for param in self.policy_net.parameters():
            param.grad.data.clamp_(-1, 1)
        self.optimizer.step()
         
                    
    def train(self, num_episodes):
        for i_episode in range(num_episodes):
            print(i_episode)
            self.env.reset()
            state = self.env.get_state()
            for _ in count():
                action = self.select_action(state)
                reward, done = self.env.step(action.item())
                reward = torch.tensor([reward], device=self.device)

                if not done:
                    next_state = self.env.get_state()
                else:
                    next_state = None

                # 메모리에 변이 저장
                self.memory.push(state, action, next_state, reward)

                # 다음 상태로 이동
                state = next_state

                # 최적화 한단계 수행(목표 네트워크에서)
                self.update_policy_net()
                if done:
                    print(self.env.get_score())
                    break
            #목표 네트워크 업데이트, 모든 웨이트와 바이어스 복사
            if i_episode % self.target_update == 0:
                self.target_net.load_state_dict(self.policy_net.state_dict())        
    
    def play(self):
        self.env.reset()
        self.env.show()
        state = self.env.get_state()
        done = False
        print("-----------------------")
        while not done:
            action = self.select_action(state, False)
            done = self.env.step(action.item())
            self.env.show()
        print("-----------------------")
                
