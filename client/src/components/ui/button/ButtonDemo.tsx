import { ArrowRight, Check, Plus, Trash2 } from 'lucide-react';
import { Button } from './Button';

export const ButtonDemo = () => {
    return (
        <div className="flex gap-4 p-4">
            {/* Button with left icon */}
            <Button icon={Plus} variant="primary">
                Add Item
            </Button>

            {/* Button with right icon */}
            <Button icon={ArrowRight} iconPosition="right" variant="neutral">
                Continue
            </Button>

            {/* Icon-only button */}
            <Button icon={Trash2} variant="danger" />

            {/* Button with different icon */}
            <Button icon={Check} variant="soft">
                Complete
            </Button>
        </div>
    );
};
