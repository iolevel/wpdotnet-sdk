<?php

/**
 * Dummy classes that are usually expected by some plugins to exist.
 * This file is never included.
 * TODO: compiler should be able to ignore classes extending something that does not exist - report warning and then fail in run time.
 */

if (!class_exists("WP_CLI_Command"))
{
    /**
     * Base class for WP-CLI commands
     *
     * @package wp-cli
     */
    abstract class WP_CLI_Command { }
}
