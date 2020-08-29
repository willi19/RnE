from collections import namedtuple
from itertools import count
import random


Transition = namedtuple('Transition', ('state', 'action', 'next_state', 'reward'))

class ReplayMemory(object):
    def __init__(self, capacity):
        self.capacity = capacity
        self.memory = []
        self.position = 0
        
    def push(self, *args):
        if len(self.memory) < self.capacity:
            self.memory.append(None)
        self.memory[self.position] = Transition(*args)
        self.position = (self.position + 1) %self.capacity
        
    def sample(self, batch_size):
        return random.sample(self.memory, batch_size)

    def __len__(self):
        return len(self.memory)
    
    def transited_sample(self, batch_size):
        sample = self.sample(batch_size)
        return Transition(*zip(*sample))

if __name__ == '__main__':
    rp = ReplayMemory(10)
    rp.push(1,2,3,4)