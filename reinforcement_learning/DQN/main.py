from dqn import DQN
from environment import ENV

env = ENV()
agent = DQN(env)

agent.train(100000)

agent.play()