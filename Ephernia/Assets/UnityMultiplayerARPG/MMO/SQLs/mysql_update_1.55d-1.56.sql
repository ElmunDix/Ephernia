START TRANSACTION;

ALTER TABLE `buildings` ADD `entityId` INT NOT NULL DEFAULT '0' AFTER `parentId`;

COMMIT;