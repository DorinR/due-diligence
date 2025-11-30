import { ArrowRightIcon, CheckIcon, PlusIcon, TrashIcon } from '@radix-ui/react-icons';
import { Button } from './Button';

export const ButtonDemo = () => {
    return (
        <div className="flex gap-4 p-4">
            {/* Button with left icon */}
            <Button icon={PlusIcon} variant="primary">
                Add Item
            </Button>

            {/* Button with right icon */}
            <Button icon={ArrowRightIcon} iconPosition="right" variant="neutral">
                Continue
            </Button>

            {/* Icon-only button */}
            <Button icon={TrashIcon} variant="danger" />

            {/* Button with different icon */}
            <Button icon={CheckIcon} variant="soft">
                Complete
            </Button>
        </div>
    );
};
