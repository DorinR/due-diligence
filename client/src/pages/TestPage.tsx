import { MinusIcon, MoonIcon, PlusIcon, SunIcon } from '@radix-ui/react-icons';
import {
  Badge,
  Box,
  Button,
  Card,
  Container,
  Flex,
  Heading,
  IconButton,
  Section,
  Select,
  Switch,
  Tabs,
  Text,
  TextArea,
  TextField,
} from '@radix-ui/themes';
import { useState } from 'react';
import { useTheme } from '../providers/ThemeProvider';

export default function TestPage() {
  const [count, setCount] = useState(0);
  const { theme, setTheme } = useTheme();

  return (
    <Container size="3">
      <Section size="3">
        <Flex justify="between" align="center" mb="6">
          <Heading as="h1" size="6">
            Radix Themes Components
          </Heading>
          <Flex align="center" gap="2">
            {theme === 'dark' ? <MoonIcon /> : <SunIcon />}
            <Switch
              checked={theme === 'dark'}
              onCheckedChange={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
              highContrast
              size="3"
            />
          </Flex>
        </Flex>

        <Card mb="6">
          <Heading as="h2" size="5" mb="4">
            Buttons
          </Heading>

          <Flex direction="column" gap="6">
            <Box>
              <Text mb="2" weight="bold">
                Variants
              </Text>
              <Flex gap="4" wrap="wrap">
                <Button>Default</Button>
                <Button variant="soft">Soft</Button>
                <Button variant="outline">Outline</Button>
                <Button variant="ghost">Ghost</Button>
              </Flex>
            </Box>

            <Box>
              <Text mb="2" weight="bold">
                Sizes
              </Text>
              <Flex gap="4" align="center" wrap="wrap">
                <Button size="1">Size 1</Button>
                <Button size="2">Size 2</Button>
                <Button size="3">Size 3</Button>
                <Button size="4">Size 4</Button>
              </Flex>
            </Box>

            <Box>
              <Text mb="2" weight="bold">
                Colors
              </Text>
              <Flex gap="4" wrap="wrap">
                <Button color="indigo">Indigo</Button>
                <Button color="blue">Blue</Button>
                <Button color="green">Green</Button>
                <Button color="orange">Orange</Button>
                <Button color="tomato">Tomato</Button>
                <Button color="red">Red</Button>
                <Button color="purple">Purple</Button>
              </Flex>
            </Box>

            <Box>
              <Text mb="2" weight="bold">
                Icon Buttons
              </Text>
              <Flex gap="4" wrap="wrap">
                <IconButton size="1">
                  <PlusIcon />
                </IconButton>
                <IconButton size="2">
                  <PlusIcon />
                </IconButton>
                <IconButton size="3">
                  <PlusIcon />
                </IconButton>
                <IconButton size="4">
                  <PlusIcon />
                </IconButton>
              </Flex>
            </Box>

            <Box>
              <Text mb="2" weight="bold">
                High Contrast
              </Text>
              <Flex gap="4" wrap="wrap">
                <Button highContrast>High Contrast</Button>
                <Button variant="soft" highContrast>
                  Soft HC
                </Button>
                <Button variant="outline" highContrast>
                  Outline HC
                </Button>
                <Button variant="ghost" highContrast>
                  Ghost HC
                </Button>
              </Flex>
            </Box>
          </Flex>
        </Card>

        <Card mb="6">
          <Heading as="h2" size="5" mb="4">
            Form Controls
          </Heading>

          <Flex direction="column" gap="6">
            <Box>
              <Text mb="2" weight="bold">
                Text Fields
              </Text>
              <Flex gap="4" wrap="wrap">
                <TextField.Root placeholder="Regular input" />
                <TextField.Root placeholder="With button">
                  <TextField.Slot side="right">
                    <Button size="1">Go</Button>
                  </TextField.Slot>
                </TextField.Root>
                <TextField.Root placeholder="Disabled" disabled />
              </Flex>
            </Box>

            <Box>
              <Text mb="2" weight="bold">
                TextArea
              </Text>
              <TextArea placeholder="Enter your message here..." />
            </Box>

            <Box>
              <Text mb="2" weight="bold">
                Select
              </Text>
              <Select.Root defaultValue="apple">
                <Select.Trigger />
                <Select.Content>
                  <Select.Item value="apple">Apple</Select.Item>
                  <Select.Item value="orange">Orange</Select.Item>
                  <Select.Item value="banana">Banana</Select.Item>
                </Select.Content>
              </Select.Root>
            </Box>
          </Flex>
        </Card>

        <Card mb="6">
          <Heading as="h2" size="5" mb="4">
            Badges & Tabs
          </Heading>

          <Flex direction="column" gap="6">
            <Box>
              <Text mb="2" weight="bold">
                Badges
              </Text>
              <Flex gap="4" wrap="wrap">
                <Badge>Default</Badge>
                <Badge variant="soft">Soft</Badge>
                <Badge variant="outline">Outline</Badge>
                <Badge variant="surface">Surface</Badge>
              </Flex>
            </Box>

            <Box>
              <Text mb="2" weight="bold">
                Tabs
              </Text>
              <Tabs.Root defaultValue="tab1">
                <Tabs.List>
                  <Tabs.Trigger value="tab1">Tab 1</Tabs.Trigger>
                  <Tabs.Trigger value="tab2">Tab 2</Tabs.Trigger>
                  <Tabs.Trigger value="tab3">Tab 3</Tabs.Trigger>
                </Tabs.List>
                <Box pt="3">
                  <Tabs.Content value="tab1">
                    <Text>Tab 1 content</Text>
                  </Tabs.Content>
                  <Tabs.Content value="tab2">
                    <Text>Tab 2 content</Text>
                  </Tabs.Content>
                  <Tabs.Content value="tab3">
                    <Text>Tab 3 content</Text>
                  </Tabs.Content>
                </Box>
              </Tabs.Root>
            </Box>
          </Flex>
        </Card>

        <Card>
          <Heading as="h2" size="5" mb="4">
            Counter Example
          </Heading>

          <Flex align="center" gap="4">
            <IconButton onClick={() => setCount(count => count - 1)} variant="soft">
              <MinusIcon />
            </IconButton>
            <Text size="5" weight="bold">
              {count}
            </Text>
            <IconButton onClick={() => setCount(count => count + 1)} variant="soft">
              <PlusIcon />
            </IconButton>
          </Flex>
        </Card>
      </Section>
    </Container>
  );
}
